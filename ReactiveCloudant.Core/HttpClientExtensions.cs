using System;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Reactive.Concurrency;
using System.IO;
using Newtonsoft.Json;
using ReactiveCloudant.Lucene;
using System.Net.Http;
using System.Text;

namespace ReactiveCloudant
{
    internal static class HttpClientExtensions
    {
        internal static IObservable<byte[]> DownloadAttachment(this HttpClient client, Uri address, string contentType = "text/plain",string username = "", string password = "")
        {
            return Observable.Create<byte[]>(async observer =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                        client.SetAuthenticationHeaders(username, password);
                    client.DefaultRequestHeaders.Add("Content-Type", contentType);
                    var response = await client.GetAsync(address);
                    if (response.IsSuccessStatusCode)
                    {                        
                        observer.OnNext(await response.Content.ReadAsByteArrayAsync());
                        observer.OnCompleted();
                    }
                    else
                    {
                        try
                        {
                            response.EnsureSuccessStatusCode();
                        }
                        catch (Exception e)
                        {
                            observer.OnError(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    observer.OnError(e);
                }
                return () => { };
            });
        }

        internal static IObservable<Attachment> Attachments(this HttpClient client, Uri address, string username = "", string password = "", bool staleok=true)
        {
            return Observable.Create<Attachment>(async observer =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                        client.SetAuthenticationHeaders(username, password);
                    client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                    var response = await client.GetAsync(address);
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            var parsed = JObject.Parse(await response.Content.ReadAsStringAsync());
                            var attachments = parsed.Property("_attachments");
                            if (attachments != null)
                            {
                                foreach (JProperty o in attachments.Value)
                                {
                                    Attachment value = new Attachment
                                    {
                                        Name = o.Name,
                                        ContentType = o.Value["content_type"].ToString()
                                    };
                                    if (int.TryParse(o.Value["length"].ToString(), out int l))
                                        value.Length = l;
                                    value.Digest = o.Value["digest"].ToString();
                                    value.Url = address.AbsoluteUri.Replace("?stale=ok", "") + "/" + value.Name;
                                    if (staleok)
                                        value.Url += "?stale=ok";
                                    observer.OnNext(value);
                                }
                            }
                            observer.OnCompleted();
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    }
                    else
                    {
                        try
                        {
                            response.EnsureSuccessStatusCode();
                        }
                        catch (Exception e)
                        {
                            observer.OnError(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    observer.OnError(e);
                }
                return () => { };
            });
        }

        internal static IObservable<string> DownloadStringAsObservable(this HttpClient client, Uri address, string username = "", string password = "", object userToken = null, IScheduler converterScheduler = null)
        {
            return Observable.Create<string>(async observer =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                        client.SetAuthenticationHeaders(username, password);
                    client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                    var response = await client.GetAsync(address);
                    if (response.IsSuccessStatusCode)
                    {
                        observer.OnNext(await response.Content.ReadAsStringAsync());
                        observer.OnCompleted();
                    }
                    else
                    {
                        try
                        {
                            response.EnsureSuccessStatusCode();
                        }
                        catch (Exception e)
                        {
                            observer.OnError(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    observer.OnError(e);
                }
                return () => { };
            });
        }

        internal static IObservable<T> DownloadAndConvertAsObservable<T>(this HttpClient client, Uri address, string username = "", string password = "", object userToken = null, IScheduler converterScheduler = null)
        {
            return Observable.Create<T>(async observer =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                        client.SetAuthenticationHeaders(username, password);
                    client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                    var response = await client.GetAsync(address);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
                        observer.OnNext(result);
                        observer.OnCompleted();
                    }
                    else
                    {
                        try
                        {
                            response.EnsureSuccessStatusCode();
                        }
                        catch (Exception e)
                        {
                            observer.OnError(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    observer.OnError(e);
                }
                return () => { };
            });
        }

        internal static IObservable<LuceneResult<T>> DownloadAndConvertAsObservable<T>(this HttpClient client, Uri address, Func<string, LuceneResult<T>> convertMethod, string username = "", string password = "", object userToken = null, IScheduler converterScheduler = null)
        {
            return Observable.Create<LuceneResult<T>>(async observer =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                        client.SetAuthenticationHeaders(username, password);
                    client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                    var response = await client.GetAsync(address);
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            var result = convertMethod(await response.Content.ReadAsStringAsync());
                            observer.OnNext(result);
                            observer.OnCompleted();
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    }
                    else
                    {
                        try
                        {
                            response.EnsureSuccessStatusCode();
                        }
                        catch (Exception e)
                        {
                            observer.OnError(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    observer.OnError(e);
                }

                return () => { };
            });
        }

        internal static IObservable<Document<T>> DownloadAndConvertDocumentAsObservable<T>(this HttpClient client, Uri address, string username = "", string password = "", object userToken = null, IScheduler converterScheduler = null)
        {
            return Observable.Create<Document<T>>(async observer =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                        client.SetAuthenticationHeaders(username, password);
                    client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                    var response = await client.GetAsync(address);
                    if (response.IsSuccessStatusCode)
                    {
                        ConvertToDocumentStream<T>(observer, await response.Content.ReadAsStringAsync(), converterScheduler);
                        observer.OnCompleted();
                    }
                    else
                    {
                        try
                        {
                            response.EnsureSuccessStatusCode();
                        }
                        catch (Exception e)
                        {
                            observer.OnError(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    observer.OnError(e);
                }

                return () => { };
            });
        }

        internal static IObservable<string> UploadStringAsObservable(this HttpClient client, Uri address, string method, string data, string username = "", string password = "", object userToken = null)
        {
            return Observable.Create<string>(async observer =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                        client.SetAuthenticationHeaders(username, password);
                    client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                    var response = await client.PostAsync(address, new StringContent(data));
                    if (response.IsSuccessStatusCode)
                    {
                        observer.OnNext(await response.Content.ReadAsStringAsync());
                        observer.OnCompleted();
                    }
                    else
                    {
                        try
                        {
                            response.EnsureSuccessStatusCode();
                        }
                        catch (Exception e)
                        {
                            observer.OnError(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    observer.OnError(e);
                }
                return () => { };            
            });
        }

        internal static IObservable<byte[]> UploadDataAsObservable(this HttpClient client, Uri address, string contentType, string method, byte[] data, string username = "", string password = "", object userToken = null)
        {
            return Observable.Create<byte[]>(async observer =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                        client.SetAuthenticationHeaders(username, password);
                    client.DefaultRequestHeaders.Add("Content-Type", contentType);
                    var response = await client.PostAsync(address, new ByteArrayContent(data));
                    if (response.IsSuccessStatusCode)
                    {
                        observer.OnNext(await response.Content.ReadAsByteArrayAsync());
                        observer.OnCompleted();
                    }
                    else
                    {
                        try
                        {
                            response.EnsureSuccessStatusCode();
                        }
                        catch (Exception e)
                        {
                            observer.OnError(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    observer.OnError(e);
                }
                return () => {  };
            });
        }

        internal static IObservable<Poll<T>> DownloadAndConvertChangesAsObservable<T>(Uri address, string username = "", string password = "", object userToken = null, IScheduler converterScheduler = null, bool includes_docs = false)
        {
            return Observable.Create<Poll<T>>(observer =>
            {
                bool disposed = false;
                try
                {
                    Task.Factory.StartNew(async () =>
                    {
                        try
                        {
                            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(address);
                            req.Headers["ContentType"] = "application/json";
                            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                                req.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                            var response = await req.GetResponseAsync();
                            using (var reader = new StreamReader(response.GetResponseStream()))
                            {
                                string line = string.Empty;
                                while ((line = reader.ReadLine()) != null && !disposed)
                                {
                                    try
                                    {
                                        if (!string.IsNullOrWhiteSpace(line))
                                        {
                                            var parsed = JObject.Parse(line);
                                            if (parsed.Property("id") == null)
                                            {
                                                if (parsed.Property("last_seq") != null)
                                                {
                                                    var seq = parsed["last_seq"].ToString();
                                                    observer.OnNext(new Poll<T> { Document = new Document<T>(string.Empty, default(T), string.Empty), Since = seq });
                                                    observer.OnCompleted();
                                                }
                                            }
                                            else
                                            {
                                                var id = parsed["id"].ToString();
                                                var seq = parsed["seq"].ToString();
                                                var revisions = parsed["changes"] as JArray;
                                                string rev = string.Empty;
                                                if (revisions != null && revisions.Count > 0)
                                                {
                                                    rev = revisions.Last()["rev"].ToString();
                                                }

                                                if (includes_docs)
                                                {
                                                    if (converterScheduler != null)
                                                    {
                                                        converterScheduler.Schedule(() =>
                                                        {
                                                            var retVal = parsed.ConvertObject<T>(includes_docs);
                                                            observer.OnNext(new Poll<T> { Document = new Document<T>(id, retVal, rev), Since = seq });
                                                        });
                                                    }
                                                    else
                                                    {
                                                        var retVal = parsed.ConvertObject<T>(includes_docs);
                                                        observer.OnNext(new Poll<T> { Document = new Document<T>(id, retVal, rev), Since = seq });
                                                    }
                                                }
                                                else
                                                {
                                                    observer.OnNext(new Poll<T> { Document = new Document<T>(id, default(T), rev), Since = seq });
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        observer.OnError(ex);
                                    }
                                }
                                observer.OnCompleted();
                            }
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    });
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                return () => { disposed = true; };
            });
        }

        internal static T ConvertObject<T>(this JObject obj, bool doc = false)
        {
            if (doc)
            {
                var prop = obj.Property("doc");
                if (prop != null)
                    return prop.Value.ToObject<T>();
                else
                    return default(T);
            }
            else
            {
                try
                {
                    var prop = obj.Property("value");
                    if (prop != null)
                    {
                        if (prop.Value is JObject)
                        {
                            return ((JObject)prop.Value).ToObject<T>();
                        }
                        else
                        {
                            return prop.Value.Value<T>();
                        }
                    }
                    else
                        return obj.Values().Value<T>();
                }
                catch
                {
                    return obj.ToObject<T>();
                }
            }
        }

        internal static void SetAuthenticationHeaders(this HttpClient client, string username, string password)
        {
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                var authInfo = username + ":" + password;
                client.DefaultRequestHeaders.Add("Authorization", "Basic "+ Convert.ToBase64String(Encoding.ASCII.GetBytes(authInfo)));
            }
        }

        private static void ConvertToDocumentStream<T>(IObserver<Document<T>> observer, string args, IScheduler converterScheduler = null)
        {
            try
            {
                if (args.StartsWith("["))
                {
                    var r = JArray.Parse(args);
                    foreach (var a in r)
                    {
                        var doc = a.ToObject<T>();
                        observer.OnNext(new Document<T>(string.Empty, doc, string.Empty));
                    }
                    observer.OnCompleted();
                }
                else
                {
                    bool getDoc = false;
                    var parsed = JObject.Parse(args);
                    var rows = parsed.Property("rows");
                    if (rows != null)
                    {
                        var first = rows.Value.FirstOrDefault(x => x.SelectToken("doc") != null);
                        if (first != null)
                            getDoc = true;

                        if (converterScheduler != null)
                        {
                            converterScheduler.Schedule(() =>
                            {
                                foreach (JObject o in rows.Value)
                                {
                                    string id = string.Empty;
                                    string rev = string.Empty;
                                    if (getDoc)
                                    {
                                        var doc = o["doc"];
                                        if (doc != null && doc is JObject)
                                        {
                                            var _id = doc["_id"];
                                            if (_id != null)
                                                id = _id.ToString();
                                            var _rev = doc["_rev"];
                                            if (_rev != null)
                                                rev = _rev.ToString();
                                        }
                                    }
                                    else
                                    {
                                        var doc = o["value"];
                                        if (doc != null)
                                        {
                                            if (doc is JObject)
                                            {
                                                var _id = doc["_id"];
                                                if (_id != null)
                                                    id = _id.ToString();
                                                var _rev = doc["_rev"];
                                                if (_rev != null)
                                                    rev = _rev.ToString();
                                            }
                                            else id = o["key"].ToString();
                                        }
                                    }
                                    T value = default(T);
                                    value = o.ConvertObject<T>(getDoc);

                                    observer.OnNext(new Document<T>(id, value, rev));
                                }
                                observer.OnCompleted();
                            });
                        }
                        else
                        {
                            foreach (JObject o in rows.Value)
                            {
                                string id = string.Empty;
                                string rev = string.Empty;
                                if (getDoc)
                                {
                                    var doc = o["doc"];
                                    if (doc != null && doc is JObject)
                                    {
                                        var _id = doc["_id"];
                                        if (_id != null)
                                            id = _id.ToString();
                                        var _rev = doc["_rev"];
                                        if (_rev != null)
                                            rev = _rev.ToString();
                                    }
                                }
                                else
                                {
                                    var doc = o["value"];
                                    if (doc != null)
                                    {
                                        if (doc is JObject)
                                        {
                                            var _id = doc["_id"];
                                            if (_id != null)
                                                id = _id.ToString();
                                            else
                                            {
                                                _id = doc["id"];
                                                if (_id != null)
                                                    id = _id.ToString();
                                            }
                                            var _rev = doc["_rev"];
                                            if (_rev != null)
                                                rev = _rev.ToString();
                                        }
                                        else
                                            id = o["key"].ToString();
                                    }

                                }
                                T value = default(T);
                                value = o.ConvertObject<T>(getDoc);
                                observer.OnNext(new Document<T>(id, value, rev));
                            }
                            observer.OnCompleted();
                        }
                    }
                    else
                    {
                        var id = parsed["_id"].ToString();
                        var rev = parsed["_rev"].ToString();

                        if (converterScheduler != null)
                            converterScheduler.Schedule(() =>
                            {
                                observer.OnNext(new Document<T>(id, parsed.ToObject<T>(), rev));
                                observer.OnCompleted();
                            });
                        else
                        {
                            observer.OnNext(new Document<T>(id, parsed.ToObject<T>(), rev));
                            observer.OnCompleted();
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        }
    }
}
