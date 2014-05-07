using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ReactiveCloudant
{
    public class CloudantSession
    {
        #region Properties

        private Subject<ProgressIndicator> progress = new Subject<ProgressIndicator>();
        /// <summary>
        /// Subscribe to the Progress for progress information while downloading or uploading data.
        /// When getting data you can specify a progressToken to filter this stream with for individual operations.
        /// </summary>
        public IObservable<ProgressIndicator> Progress
        {
            get { return progress.AsObservable(); }
        }

        /// <summary>
        /// The username for the session
        /// </summary>
        public string Username { get; private set; }
        /// <summary>
        /// The password for the session
        /// </summary>
        public string Password { get; private set; }
        /// <summary>
        /// The base url for the database i.e. https://nisbus.cloudant.com/
        /// </summary>
        public string BaseUrl { get; private set; }        

        #endregion

        #region CTOR
        
        /// <summary>
        /// Creates a new session for a cloudant url
        /// </summary>
        /// <param name="baseUrl">The url to your Cloudant account i.e. https://nisbus.cloudant.com/</param>
        /// <param name="username">Your Cloudant username (if your db is open to the public you don't need this)</param>
        /// <param name="password">Your Cloudant password (if your db is open to the public you don't need this)</param>
        public CloudantSession(string baseUrl, string username = "", string password = "")
        {
            Uri u = null;
            try
            {
                u = new Uri(baseUrl);
            }
            catch (UriFormatException formatError)
            {
                throw formatError;
            }

            BaseUrl = u.AbsoluteUri.ToString();
            Username = username;
            Password = password;
        }

        #endregion

        #region Document API
        
        /// <summary>
        /// Saves an object to the database
        /// </summary>
        /// <param name="database">The database name to save to</param>
        /// <param name="item">The item to save</param>
        /// <param name="id">The id to save the object as (optional)</param>
        /// <param name="revision_id">The revision id of the document if updating (you also need the id if you are setting the revision id)</param>
        /// <param name="progressToken">a string that you can use to filter the progress stream of the session with.</param>
        /// <returns>A successful save operation will return a single SaveResult object containing the ID and REV of the document</returns>
        /// <exception cref="ArgumentException"></exception>
        public IObservable<SaveResult> Save(string database, object item, string id = "", string revision_id = "", string progressToken = "")
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify a database to save to");
            if (item == null)
                throw new ArgumentException("Cannot save null object");

            var url = BaseUrl + database + "/";
            var json = JsonConvert.SerializeObject(item);
            json = SetID(json, id);
            if (!string.IsNullOrWhiteSpace(revision_id))
                json = SetRev(json, revision_id);
            Subject<SaveResult> saveResult = new Subject<SaveResult>();
            
            using (WebClient client = new WebClient())
            {
                client.UploadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));
                client.UploadStringAsObservable(new Uri(url), "POST", json, Username, Password).Subscribe(s =>
                    {
                        try
                        {
                            string doc_id = string.Empty;
                            string rev_id = string.Empty;
                            var response = JObject.Parse(s);
                            var idProp = response.Property("id");
                            if (idProp != null)
                            {
                                JValue val = idProp.Value as JValue;
                                doc_id = idProp.Value.ToString();
                            }

                            var revision = response.Property("rev");
                            if (revision != null)
                            {
                                JValue val = revision.Value as JValue;
                                rev_id = val.Value.ToString();
                            }
                            saveResult.OnNext(new SaveResult(doc_id, rev_id));
                        }
                        catch (Exception e) 
                        { 
                            saveResult.OnError(e); 
                        }
                    },(e) => saveResult.OnError(e),
                    () => saveResult.OnCompleted());
                return saveResult.AsObservable();
            }
        }

        /// <summary>
        /// Saves an attachment with a document
        /// </summary>
        /// <param name="database">The database to save to</param>
        /// <param name="document_id">The id of the document to attach to (if it doesn't exist it will be created)</param>
        /// <param name="contentType">The content type of the attachment</param>
        /// <param name="attachment">The attachment as bytes</param>
        /// <param name="attachmentName">The name to give the attachment</param>
        /// <param name="revision_id">If updating an existing document the revision id needs to be set</param>
        /// <param name="progressToken">a string that you can use to filter the progress stream of the session with.</param>
        /// <returns>A successful save operation will return a single SaveResult object containing the ID and REV of the document</returns>
        /// <exception cref="ArgumentException"></exception>
        public IObservable<SaveResult> SaveAttachment(string database, string document_id, string contentType, byte[] attachment, string attachmentName = "", string revision_id = "", string progressToken = "")
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify a database to save to");
            if (attachment == null)
                throw new ArgumentException("Cannot save null attachments");
            if(string.IsNullOrWhiteSpace(document_id))
                throw new ArgumentException("You must specify a document ID for the attachment");
            if(string.IsNullOrWhiteSpace(attachmentName))
                throw new ArgumentException("You must specify an attachment name");
            var url = BaseUrl + database + "/"+document_id+"/"+attachmentName;


            if (!string.IsNullOrWhiteSpace(revision_id))
                url += "?rev=" + revision_id;
            
            Subject<SaveResult> saveResult = new Subject<SaveResult>();
            using (WebClient client = new WebClient())
            {
                client.UploadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));
                client.UploadDataAsObservable(new Uri(url), contentType, "PUT", attachment, Username, Password).Subscribe(s =>
                {
                    try
                    {                        
                        var result = Encoding.UTF8.GetString(s);                                                
                        string doc_id = string.Empty;
                        string rev_id = string.Empty;
                        var response = JObject.Parse(result);
                        var idProp = response.Property("id");
                        if (idProp != null)
                        {
                            JValue val = idProp.Value as JValue;
                            doc_id = idProp.Value.ToString();
                        }

                        var revision = response.Property("rev");
                        if (revision != null)
                        {
                            JValue val = revision.Value as JValue;
                            rev_id = val.Value.ToString();
                        }
                        saveResult.OnNext(new SaveResult(doc_id, rev_id));
                        saveResult.OnCompleted();
                    }
                    catch (Exception e)
                    {
                        saveResult.OnError(e);
                    }
                }, (e) => saveResult.OnError(e),
                    () => saveResult.OnCompleted());
                return saveResult.AsObservable();
            }

        }

        /// <summary>
        /// Calls a view in the database
        /// </summary>
        /// <typeparam name="T">The type of the object to retrieve, don't use List<T> since the response will send each individual item to the stream."/></typeparam>
        /// <param name="database">The database the view belongs to</param>
        /// <param name="designDoc">The design document the view belongs to (you don't have to include the _design part</param>
        /// <param name="viewName">The name of the view to get</param>
        /// <param name="key">Optional seach key</param>
        /// <param name="startKey">Optional range argument for keys, note that key and startkey are mutually exclusive</param>
        /// <param name="endKey">Optional end range argument for keys, you must specify a startkey to use the endkey</param>
        /// <param name="includeDocs">Boolean indicating whether the docs from the view should be included in the query</param>
        /// <param name="converterScheduler">A scheduler used to execute the conversion from json to a .NET object (this can be useful when creating UI objects that need to be created on the UI thread)</param>
        /// <param name="progressToken">a string that you can use to filter the progress stream of the session with.</param>
        /// <returns>An observable sequence that will materialize as each object is deserialized</returns>
        /// <exception cref="ArgumentException"></exception>
        public IObservable<Document<T>> View<T>(string database, string designdocument, string view, string key = "", string startKey = "", string endKey="", bool includedocs = false, bool inclusiveend = false, bool descending = false, int limit = 0, int skip = 0, IScheduler converterScheduler = null, string progressToken = "")
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify the database","database");
            if (string.IsNullOrWhiteSpace(designdocument))
                throw new ArgumentException("You must specify a design document", "designdocument");
            if (string.IsNullOrWhiteSpace(view))
                throw new ArgumentException("You must specify a view name", "view");
            var url = BaseUrl +database+"/_design/"+designdocument+"/_view/"+ view;
            url += SetQueryParameters(key, startKey, endKey, includedocs, inclusiveend, descending, skip, limit);
            if (url.Contains("?"))
                url += "&stale=ok";
            else
                url += "?stale=ok";
            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));
                return client.DownloadAndConvertAsObservable<T>(new Uri(url), Username, Password, progressToken, converterScheduler: converterScheduler);
            }
        }
        
        /// <summary>
        /// Calls a view in the database
        /// </summary>
        /// <typeparam name="T">The type of the object to retrieve, don't use List<T> since the response will send each individual item to the stream."/></typeparam>
        /// <param name="document_id">The ID of the document to get</param>
        /// <param name="database">The database the view belongs to</param>
        /// <param name="converterScheduler">A scheduler used to execute the conversion from json to a .NET object (this can be useful when creating UI objects that need to be created on the UI thread)</param>
        /// <param name="progressToken">a string that you can use to filter the progress stream of the session with.</param>
        /// <returns>An observable sequence that will only send one item</returns>
        /// <exception cref="ArgumentException"></exception>
        public IObservable<Document<T>> Document<T>(string document_id, string database, IScheduler converterScheduler = null, string progressToken = "")
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify the database");

            if (string.IsNullOrWhiteSpace(document_id))
                throw new ArgumentException("documentID cannot be empty");
            var url = BaseUrl+database+"/";
            url += document_id+"?stale=ok";
            using (WebClient client = new WebClient())
            {                    
                client.DownloadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));
                return client.DownloadAndConvertAsObservable<T>(new Uri(url), Username, Password, progressToken, converterScheduler: converterScheduler);
            }
        }

        /// <summary>
        /// returns all the attachments of a document, i.e. their names, size, type etc.
        /// </summary>
        /// <param name="document_id">The id of the document to search for attachments on</param>
        /// <param name="database">The database the document is in</param>
        /// <returns cref="Attachment">An attachment which you can then query for it's actual data</returns>
        public IObservable<Attachment> Attachments(string document_id, string database)
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify the database");

            if (string.IsNullOrWhiteSpace(document_id))
                throw new ArgumentException("documentID cannot be empty");
            var url = BaseUrl + database + "/";
            url += document_id+"?stale=ok";
            using (WebClient client = new WebClient())
            {             
                return client.Attachments(new Uri(url), Username, Password);
            }
        }

        public IObservable<string> DeleteDocument(string document_id, string database, string progressToken = "")
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify a database to delete");
            if (string.IsNullOrWhiteSpace(document_id))
                throw new ArgumentException("You must specify a document id to delete");

            var url = BaseUrl + database + "/"+document_id;
            using (WebClient client = new WebClient())
            {
                client.UploadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));
                return client.UploadStringAsObservable(new Uri(url), "DELETE", "", Username, Password);
            }
        }

        #endregion

        #region Admin API

        /// <summary>
        /// Creates a new api key
        /// </summary>
        /// <param name="progressToken">a string that you can use to filter the progress stream of the session with.</param>
        /// <returns>A stream that returns the APIKey generated and includes the new username and password</returns>
        public IObservable<APIKey> CreateAPIKey(string progressToken = "")
        {
            var url = "https://cloudant.com/api/generate_api_key";
            
            Subject<APIKey> key = new Subject<APIKey>();            
            using (WebClient client = new WebClient())
            {
                client.UploadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));
                client.UploadStringAsObservable(new Uri(url), "POST", "", Username, Password).Subscribe(s =>
                    {
                        try
                        {                            
                            var json = JObject.Parse(s);
                            var password = json["password"].ToString();
                            var username = json["key"].ToString(); 
                            key.OnNext(new APIKey(username, password));
                        }
                        catch (Exception e)
                        {
                            key.OnError(e);
                        }
                    },(e) => key.OnError(e),
                    () => key.OnCompleted());
                return key.AsObservable();
            }
        }

        /// <summary>
        /// Sets permissions for a user to a database
        /// </summary>
        /// <param name="database">The database to set the permissions for</param>
        /// <param name="username">The user to set the permissions for</param>
        /// <param name="reader">Whether the user can read from the database</param>
        /// <param name="writer">Whether the user can write to the database</param>
        /// <param name="admin">Whether the user is and admin for the database</param>
        /// <param name="creator">Whether the user is the creator of the database</param>
        /// <param name="progressToken">a string that you can use to filter the progress stream of the session with.</param>
        /// <returns>a single string on the form ok {ok:true}</returns>
        public IObservable<string> SetPermissions(string database, string username, bool reader = false, bool writer = false, bool admin = false, bool creator = false, string progressToken = "")
        {
            var withoutProtocol = BaseUrl.Replace("https://", "");            
            var account = withoutProtocol.Substring(0,withoutProtocol.IndexOf('.'));
            string rolesString = "database=" + account + "/" + database + "&username=" + username;
            if (reader)
                rolesString += "&roles=_reader";
            if (writer)
                rolesString += "&roles=_writer";
            if (admin)
                rolesString += "&roles=_admin";
            if (creator)
                rolesString += "&roles=_creator";
            using (var client = new WebClient())
            {
                client.UploadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));                
                return client.UploadStringAsObservable(new Uri("https://cloudant.com/api/set_permissions"), "POST", rolesString, Username, Password, progressToken);
            }            
        }

        public IObservable<string> CreateDatabase(string database, string progressToken = "")
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify a database to create");
            var url = BaseUrl + database + "/";
            using (WebClient client = new WebClient())
            {
                client.UploadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));
                return client.UploadStringAsObservable(new Uri(url), "PUT", "", Username, Password);
            }
        }

        public IObservable<string> DeleteDatabase(string database, string progressToken = "")
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify a database to delete");
            var url = BaseUrl + database + "/";
            using (WebClient client = new WebClient())
            {
                client.UploadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));
                return client.UploadStringAsObservable(new Uri(url), "DELETE", "", Username, Password);
            }
        }

        #endregion
        
        #region Helpers

        internal string SetQueryParameters(string key, string startKey, string endKey, bool includeDocs, bool inclusiveend, bool descending, int skip, int limit)
        {
            var returnValue = string.Empty;
            string include = string.Empty;
            string s = string.Empty;
            string l = string.Empty;
            string docs = string.Empty;
            string desc = string.Empty;
            if (inclusiveend)
                include = "inclusive_end=true";
            if (skip > 0)
                s = "skip=" + skip;
            if (limit > 0)
                l = "limit=" + limit;
            if(includeDocs)
                docs = "include_docs=true";
            if (descending)
                desc = "descending=true";
            var keys = CreateKeyQuery(key, startKey, endKey);
            string parameters = string.Join("&", new List<string>{ include, s, l, docs }).Trim('&');
            parameters = string.Join("&", new List<string> { parameters, keys }).Trim('&');
            if(!string.IsNullOrWhiteSpace(parameters) )
                returnValue += "?"+parameters;
            return returnValue;
        }

        internal string CreateKeyQuery(string key, string startKey, string endKey)
        {
            if (!string.IsNullOrWhiteSpace(key) && (!string.IsNullOrWhiteSpace(startKey) || !string.IsNullOrWhiteSpace(endKey)))
                throw new ArgumentException("Key and StartKey/EndKey are mutually exclusive","key");
            if (!string.IsNullOrWhiteSpace(key))
                return "key=\"" + key + "\"";
            if (!string.IsNullOrWhiteSpace(startKey) && !string.IsNullOrWhiteSpace(endKey))
                return "startkey=\"" + startKey + "\"&endkey=\"" + endKey + "\"";
            else if (!string.IsNullOrWhiteSpace(startKey))
                return "startkey=\"" + startKey + "\"";
            else if (!string.IsNullOrWhiteSpace(endKey))
                throw new ArgumentException("You need to specify startkey as well when specifying endkey","endkey");
            else
                return string.Empty;
        }

        internal string SetRev(string json, string id)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Invalid json for setting revision ID", "json");
            if (string.IsNullOrWhiteSpace(id))
                return json;

            return json.Insert(1, "\"_rev\":\"" + id + "\",");
        }

        internal string SetID(string json, string id)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Invalid json for saving", "json");
            if (string.IsNullOrWhiteSpace(id))
                id = GetID();
            return json.Insert(1, "\"_id\":\"" + id + "\",");
        }

        internal string GetID()
        {
            using (var client = new WebClient())
            {
                var ids = client.DownloadString(BaseUrl + "_uuids");
                var json = JObject.Parse(ids);
                return json.Property("uuids").Value.FirstOrDefault().ToString();               
            }
        }

        #endregion
    }    
}
