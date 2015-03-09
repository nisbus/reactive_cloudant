using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
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
        /// Gets a single document from the database
        /// </summary>
        /// <typeparam name="T">The type of the object to retrieve, don't use List<T> since the response will send each individual item to the stream."/></typeparam>
        /// <param name="document_id">The ID of the document to get</param>
        /// <param name="database">The database the view belongs to</param>
        /// <param name="converterScheduler">A scheduler used to execute the conversion from json to a .NET object (this can be useful when creating UI objects that need to be created on the UI thread)</param>
        /// <param name="progressToken">a string that you can use to filter the progress stream of the session with.</param>
        /// <param name="staleok">Whether stale documents are ok</param>
        /// <returns>An observable sequence that will only send one item</returns>
        /// <exception cref="ArgumentException"></exception>
        public IObservable<Document<T>> Document<T>(string document_id, string database, IScheduler converterScheduler = null, string progressToken = "", bool staleok = true)
        {
            var url = CreateGetUrl(document_id, database, staleok);
            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));
                return client.DownloadAndConvertDocumentAsObservable<T>(new Uri(url), Username, Password, progressToken, converterScheduler: converterScheduler);
            }
        }

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
            string json = string.Empty;
            if (item is string)
                json = (string)item;
            else
                json = JsonConvert.SerializeObject(item);
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
                        string error = string.Empty;

                        var response = JObject.Parse(s);
                        var e = response.Property("error");
                        if (e != null)
                        {
                            var reason = response.Property("reason");
                            error = e.Value.ToString() + " - " + reason.Value.ToString();
                        }
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
                        saveResult.OnNext(new SaveResult(doc_id, rev_id, error));
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
        /// Deletes a document
        /// </summary>
        /// <param name="document_id">The id of the document</param>
        /// <param name="database">The database to delete from</param>
        /// <param name="revision_id">The revision id of the document</param>
        /// <param name="progressToken">a string that you can use to filter the progress stream of the session with.</param>
        /// <returns>A string with the results of the operation, success = {"ok":true} </returns>
        public IObservable<string> DeleteDocument(string document_id, string database, string revision_id, string progressToken = "")
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify a database to delete");
            if (string.IsNullOrWhiteSpace(document_id))
                throw new ArgumentException("You must specify a document id to delete");
            if (string.IsNullOrWhiteSpace(revision_id))
                throw new ArgumentException("You must specify the revision id of the document");

            var url = BaseUrl + database + "/" + document_id;
            url += "?rev=" + revision_id;
            using (WebClient client = new WebClient())
            {
                client.UploadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));
                return client.UploadStringAsObservable(new Uri(url), "DELETE", "", Username, Password);
            }
        }

        /// <summary>
        /// Saves an object to the database
        /// </summary>
        /// <param name="database">The database name to delete from</param>
        /// <param name="documents">The documents to delete</param>
        /// <param name="progressToken">a string that you can use to filter the progress stream of the session with.</param>
        /// <returns>A successful save operation will a SaveResult object for each document saved</returns>
        /// <exception cref="ArgumentException"></exception>
        public IObservable<SaveResult> BulkDelete(string database, IList<Document<dynamic>> documents, string progressToken = "")
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify a database to delete from");
            if (documents == null)
                throw new ArgumentException("Cannot delete null object");

            var url = BaseUrl + database + "/_bulk_docs";
            string sb = "{\"docs\": [";
            foreach (var d in documents)
            {
                if (string.IsNullOrWhiteSpace(d.ID) || string.IsNullOrWhiteSpace(d.Version))
                    throw new ArgumentException("All documents need to have both version and id to be deleted");

                string json = JsonConvert.SerializeObject(d.Item);
                json = SetID(json, d.ID);                
                json = SetRev(json, d.Version);
                json = SetDeleted(json);
                sb += json + ",";

            }
            sb = sb.TrimEnd(new char[] { ',' }) + "]}";
            Subject<SaveResult> deleteResult = new Subject<SaveResult>();

            using (WebClient client = new WebClient())
            {
                client.UploadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));

                client.UploadStringAsObservable(new Uri(url), "POST", sb, Username, Password).Subscribe(s =>
                {
                    try
                    {
                        string doc_id = string.Empty;
                        string rev_id = string.Empty;
                        string error = string.Empty;
                        var responses = JArray.Parse(s);
                        foreach (JObject response in responses)
                        {
                            var e = response.Property("error");
                            if (e != null)
                            {
                                var reason = response.Property("reason").Value.ToString();
                                error = e.Value.ToString() + " - " + reason;
                            }
                            else
                            {
                                var revision = response.Property("rev");
                                if (revision != null)
                                {
                                    JValue val = revision.Value as JValue;
                                    rev_id = val.Value.ToString();
                                }
                            }
                            var idProp = response.Property("id");
                            if (idProp != null)
                            {
                                JValue val = idProp.Value as JValue;
                                doc_id = idProp.Value.ToString();
                            }
                            deleteResult.OnNext(new SaveResult(doc_id, rev_id, error));
                            error = string.Empty;
                        }
                    }
                    catch (Exception e)
                    {
                        deleteResult.OnError(e);
                    }
                }, (e) => deleteResult.OnError(e),
                    () => deleteResult.OnCompleted());
                return deleteResult.AsObservable();
            }
        }

        /// <summary>
        /// Saves an object to the database
        /// </summary>
        /// <param name="database">The database name to save/update to</param>
        /// <param name="documents">The documents to save</param>
        /// <param name="progressToken">a string that you can use to filter the progress stream of the session with.</param>
        /// <returns>A successful save operation will a SaveResult object for each document saved</returns>
        /// <exception cref="ArgumentException"></exception>
        public IObservable<SaveResult> BulkSave(string database, IList<Document<dynamic>> documents, string progressToken = "")
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify a database to save to");
            if (documents == null)
                throw new ArgumentException("Cannot save null object");

            var url = BaseUrl + database + "/_bulk_docs";
            string sb = "{\"docs\": [";            
            foreach (var d in documents)
            {
                string json = string.Empty;
                if (d.Item is string)
                    json = d.Item;
                else
                    json = JsonConvert.SerializeObject(d.Item);
                if(!string.IsNullOrWhiteSpace(d.ID))
                    json = SetID(json, d.ID);

                if (!string.IsNullOrWhiteSpace(d.Version))
                    json = SetRev(json, d.Version);
                sb += json+",";
            }
            sb = sb.TrimEnd(new char[]{','})+ "]}";
            Subject<SaveResult> saveResult = new Subject<SaveResult>();

            using (WebClient client = new WebClient())
            {
                client.UploadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));

                client.UploadStringAsObservable(new Uri(url), "POST", sb, Username, Password).Subscribe(s =>
                {
                    try
                    {
                        string doc_id = string.Empty;
                        string rev_id = string.Empty;
                        string error = string.Empty;                        
                        var responses = JArray.Parse(s);
                        foreach (JObject response in responses)
                        {
                            var e = response.Property("error");
                            if (e != null)
                            {                                
                                var reason = response.Property("reason").Value.ToString();
                                error = e.Value.ToString()+" - "+reason;
                            }
                            else
                            {
                                var revision = response.Property("rev");
                                if (revision != null)
                                {
                                    JValue val = revision.Value as JValue;
                                    rev_id = val.Value.ToString();
                                }
                            }
                            var idProp = response.Property("id");
                            if (idProp != null)
                            {
                                JValue val = idProp.Value as JValue;
                                doc_id = idProp.Value.ToString();
                            }
                            saveResult.OnNext(new SaveResult(doc_id, rev_id,error));
                            error = string.Empty;
                        }
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
        /// <param name="staleok">Whether stale documents are ok</param>        
        /// <param name="reduce">Whether the view should be reduced</param>
        /// <param name="group">Whether to group the results or not</param>
        /// <returns>An observable sequence that will materialize as each object is deserialized</returns>
        /// <exception cref="ArgumentException"></exception>
        public IObservable<Document<T>> View<T>(string database, string designdocument, string view, string key = "", string startKey = "", string endKey = "", bool includedocs = false, bool inclusiveend = false, bool descending = false, int limit = 0, int skip = 0, IScheduler converterScheduler = null, string progressToken = "", bool staleok = true, int group_level = 0, bool reduce = false, bool group = false)
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify the database","database");
            if (string.IsNullOrWhiteSpace(designdocument))
                throw new ArgumentException("You must specify a design document", "designdocument");
            if (string.IsNullOrWhiteSpace(view))
                throw new ArgumentException("You must specify a view name", "view");
            var url = BaseUrl +database+"/_design/"+designdocument+"/_view/"+ view;
            url += SetViewParameters(key, startKey, endKey, includedocs, inclusiveend, descending, skip, limit, group_level, reduce, group);
            if (staleok)
            {
                if (url.Contains("?"))
                    url += "&stale=ok";
                else
                    url += "?stale=ok";
            }
            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));
                return client.DownloadAndConvertDocumentAsObservable<T>(new Uri(url), Username, Password, progressToken, converterScheduler: converterScheduler);
            }
        }

        /// <summary>
        /// Calls a list function in the database for the specified view
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
        /// <param name="staleok">Whether stale documents are ok</param>
        /// <param name="requestParams">Additional request parameters in the form paramname=paramvalue</param>
        /// <returns>An observable sequence that will materialize as each object is deserialized</returns>
        /// <exception cref="ArgumentException"></exception>
        public IObservable<Document<T>> List<T>(string database, string designdocument, string list, string view, 
                                                string key = "", string startKey = "", string endKey = "", bool includedocs = false, 
                                                bool inclusiveend = false, bool descending = false, int limit = 0, int skip = 0, 
                                                IScheduler converterScheduler = null, string progressToken = "", 
                                                bool staleok = true, string requestParams = null)
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify the database", "database");
            if (string.IsNullOrWhiteSpace(designdocument))
                throw new ArgumentException("You must specify a design document", "designdocument");
            if (string.IsNullOrWhiteSpace(view))
                throw new ArgumentException("You must specify a view name", "view");
            if (string.IsNullOrWhiteSpace(list))
                throw new ArgumentException("You must specify a list name", "list");

            var url = BaseUrl + database + "/_design/" + designdocument + "_list/"+list+"/" + view;
            url += SetQueryParameters(key, startKey, endKey, includedocs, inclusiveend, descending, skip, limit);
            if (staleok)
            {
                if (url.Contains("?"))
                    url += "&stale=ok";
                else
                    url += "?stale=ok";
            }
            if (!string.IsNullOrWhiteSpace(requestParams))
            {
                if (url.Contains("?"))
                    url += "&" + requestParams;
                else
                    url += "?" + requestParams;
            }
            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));
                return client.DownloadAndConvertDocumentAsObservable<T>(new Uri(url), Username, Password, progressToken, converterScheduler: converterScheduler);
            }
        }

        /// <summary>
        /// Calls a list function in the database for the specified view
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
        /// <param name="staleok">Whether stale documents are ok</param>
        /// <param name="requestParams">Additional request parameters in the form paramname=paramvalue</param>
        /// <returns>An observable sequence that will materialize as each object is deserialized</returns>
        /// <exception cref="ArgumentException"></exception>
        public IObservable<Document<T>> Show<T>(string database, string designdocument, string show, string document_id,
                                                string key = "", string startKey = "", string endKey = "", bool includedocs = false,
                                                bool inclusiveend = false, bool descending = false, int limit = 0, int skip = 0,
                                                IScheduler converterScheduler = null, string progressToken = "",
                                                bool staleok = true, string requestParams = null)
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify the database", "database");
            if (string.IsNullOrWhiteSpace(designdocument))
                throw new ArgumentException("You must specify a design document", "designdocument");
            if (string.IsNullOrWhiteSpace(show))
                throw new ArgumentException("You must specify a show name", "list");

            var url = BaseUrl + database + "/_design/" + designdocument + "_show/" + show + "/" + document_id;
            url += SetQueryParameters(key, startKey, endKey, includedocs, inclusiveend, descending, skip, limit);
            if (staleok)
            {
                if (url.Contains("?"))
                    url += "&stale=ok";
                else
                    url += "?stale=ok";
            }
            if (!string.IsNullOrWhiteSpace(requestParams))
            {
                if (url.Contains("?"))
                    url += "&" + requestParams;
                else
                    url += "?" + requestParams;
            }
            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));
                return client.DownloadAndConvertDocumentAsObservable<T>(new Uri(url), Username, Password, progressToken, converterScheduler: converterScheduler);
            }
        }

        #endregion

        #region Attachment API

        /// <summary>
        /// returns all the attachments of a document, i.e. their names, size, type etc.
        /// </summary>
        /// <param name="document_id">The id of the document to search for attachments on</param>
        /// <param name="database">The database the document is in</param>
        /// <param name="staleok">Wheter stale attachments should be returned</param>
        /// <returns cref="Attachment">An attachment which you can then query for it's actual data</returns>
        public IObservable<Attachment> Attachments(string document_id, string database, bool staleok = true)
        {
            var url = CreateGetUrl(document_id, database, staleok);
            using (WebClient client = new WebClient())
            {
                return client.Attachments(new Uri(url), Username, Password);
            }
        }


        public IObservable<byte[]> Attachment(string document_id, string attachment_name, string database, bool staleok = true)
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify the database");

            if (string.IsNullOrWhiteSpace(document_id))
                throw new ArgumentException("documentID cannot be empty");
            var url = BaseUrl + database + "/";
            if (staleok)
                url += document_id + "/" + attachment_name + "?stale=ok";
            else
                url += document_id + "/" + attachment_name;
            using (WebClient client = new WebClient())
            {
                return client.DownloadAttachment(new Uri(url), username: Username, password: Password);
            }
        }

        /// <summary>
        /// returns all the attachments of a document, i.e. their names, size, type etc.
        /// </summary>
        /// <param name="document_id">The id of the document to search for attachments on</param>
        /// <param name="database">The database the document is in</param>
        /// <param name="staleok">Whether to return stale attachments</param>
        /// <param name="attachment_name">a filter to get just an attachment with the name</param>
        /// <returns cref="Attachment">The actual byte data of the attachment/s</returns>
        public IObservable<byte[]> Attachments(string document_id, string database, bool staleok = true, string attachment_name = null)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            return Observable.Create<byte[]>(observer =>
            {
                var url = CreateGetUrl(document_id, database, staleok);
                using (WebClient client = new WebClient())
                {
                    if (attachment_name != null)
                    {
                        client.Attachments(new Uri(url), Username, Password).Where(a => a.Name == attachment_name).Subscribe(async attachment =>
                        {
                            if (attachment != null)
                            {
                                observer.OnNext(await attachment.Data(Username, Password).RunAsync(source.Token));
                            }
                            else
                                observer.OnCompleted();
                        }, (e) => observer.OnError(e),
                        () => observer.OnCompleted());
                    }
                    else
                    {
                        client.Attachments(new Uri(url), Username, Password).Subscribe(async attachment =>
                        {
                            if (attachment != null)
                                observer.OnNext(await attachment.Data(Username, Password).RunAsync(source.Token));
                        }, (e) => observer.OnError(e),
                        () => observer.OnCompleted());
                    }
                }
                return () => { source.Cancel(); };
            });
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
            if (string.IsNullOrWhiteSpace(document_id))
                throw new ArgumentException("You must specify a document ID for the attachment");
            if (string.IsNullOrWhiteSpace(attachmentName))
                throw new ArgumentException("You must specify an attachment name");
            var url = BaseUrl + database + "/" + document_id + "/" + attachmentName;


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

        #endregion

        #region Changes feed API

        /// <summary>
        /// Subscribes to the changes feed of a database
        /// </summary>
        /// <typeparam name="T">The type of object to listen for, other types will be ignored</typeparam>
        /// <param name="database">The database to listen to</param>
        /// <param name="document_ids">A list of document id's to filter on</param>
        /// <param name="filter">A filter described in the design document</param>
        /// <param name="include_docs">Whether to include the docs being inserted/updated/deleted</param>
        /// <param name="heartbeat">Heartbeat for the connection</param>
        /// <param name="limit">A limit to the documents returned</param>
        /// <param name="since">The sequence number to start listening from</param>
        /// <param name="timeout">A timeout for stopping the listener</param>
        /// <param name="descending">Whether documents should be ordered in a descending order when returned</param>
        /// <param name="convererScheduler">A scheduler to create the objects on, useful for UI objects in for example WPF</param>
        /// <returns>A Poll<T> object which contains the document and the last sequence number"/></returns>
        public IObservable<Poll<T>> Changes<T>(string database, IList<string> document_ids = null, string filter = null, bool include_docs = false, int? heartbeat = null, int? limit = null, string since = null, int? timeout = null, bool descending = false, IScheduler convererScheduler = null)
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify a database for the feed");
            var url = BaseUrl + database + "/_changes" + ParseChangesParameters(heartbeat, limit, since, timeout, descending, filter, include_docs, document_ids);
            Subject<Poll<T>> outer = new Subject<Poll<T>>();
            Poll<T>(since, include_docs, convererScheduler, url, outer);
            return outer.AsObservable();
        }
               
        #endregion

        #region Cloudant Query API

        /// <summary>
        /// Get all indexes defined for a particular database.
        /// This only lists Cloudant Query indexes and not Search indexes
        /// </summary>
        /// <param name="database">The database to query for indexes</param>
        /// <returns></returns>
        public IObservable<Index> ListIndexes(string database)
        {
            return Observable.Create<Index>(observer =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(database))
                        throw new ArgumentException("You must specify a database to create");
                    var url = BaseUrl + database + "/_index";
                    using (WebClient client = new WebClient())
                    {
                        client.DownloadAndConvertAsObservable<IndexList>(new Uri(url), Username, Password).Subscribe(index =>
                        {
                            foreach (var i in index.Indexes)
                            {
                                observer.OnNext(i);
                            }
                        },
                            (e) => observer.OnError(e),
                            () => observer.OnCompleted());
                    }
                }
                catch (Exception error)
                {
                    observer.OnError(error);
                }
                return () => { };
            });
        }

        /// <summary>
        /// Create a new Cloudant Query index.
        /// </summary>
        /// <param name="database">The database to index</param>
        /// <param name="indexToSave">The index definition</param>
        /// <param name="overwrite">Indicates whether to overwrite existing indexes if found (default false)</param>
        /// <returns>A boolean indicating the success of the operation</returns>
        public IObservable<bool> CreateIndex(string database, Index indexToSave, bool overwrite = false)
        {
            return Observable.Create<bool>(observer =>
            {
                try
                {
                    int c = 0;
                    var body = "{\"index\": { \"fields\": [";
                    foreach (var i in indexToSave.Fields)
                    {
                        if (c >= 1)
                            body += ",";
                        body += "{\"" + i.FieldName + "\":\"" + i.SortOrder + "\"}";
                    }
                    body += "]}";
                    if (!string.IsNullOrWhiteSpace(indexToSave.Name))
                        body += ",\"name\": \"" + indexToSave.Name + "\"";
                    if (!string.IsNullOrWhiteSpace(indexToSave.DesignDoc))
                        body += ",\"ddoc\": \"" + indexToSave.DesignDoc + "\"";
                    body += ",\"type\":\"json\"}";

                    string url = BaseUrl + database + "/_index";
                    using (WebClient client = new WebClient())
                    {
                        client.UploadStringAsObservable(new Uri(url), "POST", body, Username, Password).Subscribe(result =>
                        {
                            var saved = JObject.Parse(result);
                            var r = saved["result"].ToString();
                            if (r.ToLowerInvariant() == "created")
                            {
                                observer.OnNext(true);
                                observer.OnCompleted();
                            }
                            else if (r.ToLowerInvariant() == "exists")
                            {
                                if (overwrite)
                                {
                                    DeleteIndex(database, indexToSave.DesignDoc, indexToSave.Name).Subscribe(deleteResult =>
                                    {
                                        var deleted = JObject.Parse(deleteResult);
                                        bool success = deleted["ok"].Value<bool>();
                                        if (success)
                                        {
                                            CreateIndex(database, indexToSave, false).Subscribe(overwritten =>
                                            {
                                                observer.OnNext(overwritten);

                                            }, (e) => observer.OnError(e),
                                                () => observer.OnCompleted());
                                        }

                                    },
                                        (e) => observer.OnError(e));
                                }
                                else
                                {
                                    observer.OnNext(false);
                                    observer.OnCompleted();
                                }
                            }
                        }, (e) => observer.OnError(e));
                    }
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
                return () => { };
            });
        }

        /// <summary>
        /// Delete a Cloudant Query index from a database
        /// </summary>
        /// <param name="database">The database to delete the index from</param>
        /// <param name="designDoc">Name of the design document to create the index in (Do not include _design)</param>
        /// <param name="indexName">Name of the index to delete</param>
        /// <returns></returns>
        public IObservable<string> DeleteIndex(string database, string designDoc, string indexName)
        {
            string url = BaseUrl + database + "/_index/" + designDoc + "/json/" + indexName;
            using (WebClient client = new WebClient())
            {
                return client.UploadStringAsObservable(new Uri(url), "DELETE", "", Username, Password);
            }
        }

        /// <summary>
        /// Executes a Cloudant Query against the specified database.        
        /// </summary>
        /// <typeparam name="T">The type to serialize the response to</typeparam>
        /// <param name="database">The database to query</param>
        /// <param name="selector">The selector as a string (see http://docs.cloudant.com/api.html#selector-syntax)</param>
        /// <param name="returnFields">The fields to return from the query, leave empty to get all of the data</param>
        /// <param name="limit">Limit the number of rows to return</param>
        /// <returns>A stream of T from the index query</returns>
        public IObservable<T> QueryIndex<T>(string database, string selector, List<string> returnFields = null, int? limit = null, int? skip = null, int readQuorum = 1, List<IndexField> sorting = null)
        {
            return Observable.Create<T>(observer =>
                {
                    string url = BaseUrl + database + "/_find/";
                    var query = BuildCloudantQuery(selector, returnFields, limit: limit, sorting:sorting, skip:skip, readQuorum: readQuorum);
                    using (WebClient client = new WebClient())
                    {
                        client.UploadStringAsObservable(new Uri(url), "POST", query, Username, Password).Subscribe(result =>
                        {
                            var docs = JObject.Parse(result);
                            try
                            {
                                var docPart = docs["docs"] as JArray;
                                foreach (var d in docPart)
                                {
                                    observer.OnNext(d.ToObject<T>());
                                }
                            }
                            catch(Exception error)
                            {
                                observer.OnError(error);
                            }
                        },
                        (e) => observer.OnError(e),
                        () => observer.OnCompleted());
                    }

                    return () => { };
                });
        }        

        #endregion

        #region Lucene Search API ****TODO****

        #endregion

        #region Admin API

        /// <summary>
        /// Creates a new api key
        /// </summary>
        /// <param name="progressToken">a string that you can use to filter the progress stream of the session with.</param>
        /// <returns>A stream that returns the APIKey generated and includes the new username and password</returns>
        public IObservable<APIKey> CreateAPIKey(string progressToken = "")
        {
            var url = "https://cloudant.com/_api/v2/api_keys";
            
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

        /// <summary>
        /// Creates a new database
        /// </summary>
        /// <param name="database">The name of the new database</param>
        /// <param name="progressToken">a string that you can use to filter the progress stream of the session with.</param>
        /// <returns>A string containing the json response from the server</returns>
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

        /// <summary>
        /// Deletes an existing database
        /// </summary>
        /// <param name="database">The name of the database to delete</param>
        /// <param name="progressToken">a string that you can use to filter the progress stream of the session with.</param>
        /// <returns>A string containing the json response from the server</returns>
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

        /// <summary>
        /// Gets a list of all databases available for this account
        /// </summary>
        /// <returns>A list of database names</returns>
        public IObservable<string> Databases()
        {
            using (WebClient client = new WebClient())
            {
                var url = new Uri(BaseUrl + "_all_dbs");
                return client.DownloadAndConvertDocumentAsObservable<string>(url, Username, Password).Select(x => x.Item);
            }
        }

        #endregion
        
        #region Helpers

        internal string ParseChangesParameters(int? heartbeat, int? limit, string since, int? timeout, bool descending, string filter, bool include_docs, IList<string> document_ids)
        {
            string returnValue = "?feed=continuous";
            returnValue += heartbeat.HasValue ? "&heartbeat=" + heartbeat.ToString() : string.Empty;
            returnValue += limit.HasValue ? "&limit=" + limit.ToString() : string.Empty;
            returnValue += !string.IsNullOrWhiteSpace(since) ? "&since=" + since : string.Empty;
            returnValue += timeout.HasValue ? "&timeout=" + timeout.ToString() : string.Empty;
            returnValue += descending ? "&descending=true" : string.Empty;
            returnValue += include_docs ? "&include_docs=true" : string.Empty;
            returnValue += !string.IsNullOrWhiteSpace(filter) ? "&filter=" + filter : string.Empty;
            if (document_ids != null && document_ids.Count > 0)
            {
                returnValue += "&document_ids=[";
                foreach (var doc_id in document_ids)
                {
                    returnValue += doc_id + ",";
                }
                if (returnValue.EndsWith(","))
                    returnValue = returnValue.TrimEnd(new char[] { ',' });
                returnValue += "]";
            }

            return returnValue;
        }

        internal void Poll<T>(string since, bool include_docs, IScheduler convererScheduler, string url, Subject<Poll<T>> outer)
        {
            string last = since;
            var inner = Observable.Create<Document<T>>(observer =>
            {
                WebClientExtensions.DownloadAndConvertChangesAsObservable<T>(new Uri(url), Username, Password, converterScheduler: convererScheduler, includes_docs: include_docs)
                    .Subscribe((data) =>
                    {
                        last = data.Since;
                        outer.OnNext(data);
                    },
                    (e) => outer.OnError(e),
                    () => { });
                return () => { };
            });
            inner.Subscribe(_ => { }, (e) => outer.OnError(e));
        }

        internal string CreateGetUrl(string document_id, string database, bool staleok)
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify the database");

            if (string.IsNullOrWhiteSpace(document_id))
                throw new ArgumentException("documentID cannot be empty");
            var url = BaseUrl + database + "/";
            if (staleok)
                url += document_id + "?stale=ok";
            else
                url += document_id;
            return url;
        }

        internal string SetViewParameters(string key, string startKey, string endKey, bool includeDocs, bool inclusiveend, bool descending, int skip, int limit, int group_level, bool reduce = false, bool group = false)
        {
            var returnValue = SetQueryParameters(key, startKey, endKey, includeDocs, inclusiveend, descending, skip, limit);
            if (group || !reduce)
            {
                string add = string.Empty;
                if (returnValue.Contains("?"))
                    add = "&";
                else add = "?";
                if (group)
                {
                    returnValue += add + "group=true";
                    add = "&";
                    if (group_level > 0)
                        returnValue += "&group_level=" + group_level.ToString();
                }
                else
                {

                    if (!reduce)
                    {
                        returnValue += add + "reduce=false";
                    }
                    else
                    {
                        returnValue += add + "reduce=true";
                    }
                }
            }

            return returnValue;
        }

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

        internal string SetDeleted(dynamic json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Invalid json for setting deleted", "json");
            return json.Insert(1, "\"_deleted\": true,");
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

        internal string BuildCloudantQuery(string selector, List<string> returnFields, int? limit, int? skip = null, int readQuorum = 1, List<IndexField> sorting = null)
        {
            string query = selector;
            if (returnFields != null && returnFields.Count > 0)
            {
                query += ",\"fields\": [";
                foreach (var f in returnFields)
                    query += "\""+f+"\",";
                query = query.TrimEnd(',');
                query += "]";
            }

            if (limit.HasValue && limit > 0)
            {
                query += ",\"limit\":" + limit.Value;
            }
            if (skip.HasValue && skip.Value > 0)
                query += ",\"skip\":" + skip.Value;

            if (sorting != null && sorting.Count > 0)
            {

                query += ",\"sort\": [";
                foreach (var s in sorting)
                    query += "{\"" + s.FieldName + "\": \""+s.SortOrder+"\"},";
                query = query.TrimEnd(',');
                query += "]"; 
            }
            query += "}";
            return query;
        }

        #endregion
    }    
}
