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

        #region API
        
        /// <summary>
        /// Saves an object to the database
        /// </summary>
        /// <param name="database">The database name to save to</param>
        /// <param name="item">The item to save</param>
        /// <param name="id">The id to save the object as (optional)</param>
        /// <param name="revision_id">The revision id of the document if updating (you also need the id if you are setting the revision id)</param>
        /// <param name="progressToken">a string that you can use to filter the progress stream of the session with.</param>
        /// <returns>A successful save operation will send two strings to the returned stream, the first is the ID of the new document and the second is the revision number</returns>
        /// <exception cref="ArgumentException"></exception>
        public IObservable<string> Save(string database, object item, string id = "", string revision_id = "", string progressToken = "")
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

            using (WebClient client = new WebClient())
            {
                client.UploadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));
                return client.UploadStringAsObservable(new Uri(url), "POST", json, Username, Password);
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
        public IObservable<T> View<T>(string database, string designdocument, string view, string key = "", string startKey = "", string endKey="", bool includedocs = false, bool inclusiveend = false, int limit, int skip, IScheduler converterScheduler = null, string progressToken = "")
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify the database","database");
            if (string.IsNullOrWhiteSpace(designdocument))
                throw new ArgumentException("You must specify a design document", "designdocument");
            if (string.IsNullOrWhiteSpace(view))
                throw new ArgumentException("You must specify a view name", "view");
            var url = BaseUrl +database+"/_design/"+designdocument+"/_view/"+ view;
            url += SetQueryParameters(key, startKey, endKey, includedocs);
            url += SetLimitsAndSkips(inclusiveend, skip, limit);
            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));
                return client.DownloadAndConvertAsObservable<T>(new Uri(url), Username, Password, progressToken, converterScheduler: converterScheduler);
            }
        }

        private string SetLimitsAndSkips(bool inclusiveend, int skip, int limit)
        {
            string retVal = string.Empty;
            if (inclusiveend && skip == 0 && limit == 0)
                return "inclusive_end=true";
            else if (inclusiveend)
                retVal += "inclusive_end=true";
            if (skip > 0)
                retVal += "skip="+skip;
            if (limit > 0)
                retVal += "limit=" + limit;
            return string.Empty;
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
        public IObservable<T> Document<T>(string document_id, string database, IScheduler converterScheduler = null, string progressToken = "")
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("You must specify the database");

            if (string.IsNullOrWhiteSpace(document_id))
                throw new ArgumentException("documentID cannot be empty");
            var url = BaseUrl+database+"/";
            url += document_id;
            using (WebClient client = new WebClient())
            {                    
                client.DownloadProgressChangedAsObservable(progressToken).Subscribe((pg) => progress.OnNext(pg));
                return client.DownloadAndConvertAsObservable<T>(new Uri(url), Username, Password, progressToken, converterScheduler: converterScheduler);
            }
        }

        #endregion

        #region Helpers
        
        internal string SetQueryParameters(string key, string startKey, string endKey, bool includeDocs)
        {
            var returnValue = string.Empty;
            var keys = CreateKeyQuery(key, startKey, endKey);
            if (!string.IsNullOrWhiteSpace(key) || !string.IsNullOrWhiteSpace(startKey) || !string.IsNullOrWhiteSpace(endKey) || includeDocs)
                returnValue += "?";
            if (includeDocs && !string.IsNullOrWhiteSpace(keys))
                returnValue += "include_docs=true&" + keys;
            else if(includeDocs)
                returnValue += "include_docs=true";
            else
                returnValue += keys;
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
