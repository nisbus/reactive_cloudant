﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;

namespace ReactiveCloudant
{
    internal static class WebClientExtensions
    {
        internal static IObservable<byte[]> DownloadAttachment(this WebClient client, Uri address, string contentType = "text/plain",string username = "", string password = "")
        {
            return Observable.Create<byte[]>(observer =>
            {
                DownloadDataCompletedEventHandler handler = (sender, args) =>
                {
                    if (args.Cancelled)
                        observer.OnCompleted();
                    else if (args.Error != null)
                        observer.OnError(args.Error);
                    else
                    {
                        try
                        {
                            observer.OnNext(args.Result);
                            observer.OnCompleted();
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    }
                };

                client.DownloadDataCompleted += handler;
                try
                {
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                        client.SetAuthenticationHeaders(username, password);
                    client.Headers.Add(HttpRequestHeader.ContentType, contentType);
                    client.DownloadDataAsync(address);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                return () => client.DownloadDataCompleted -= handler;
            });
        }

        internal static IObservable<Attachment> Attachments(this WebClient client, Uri address, string username = "", string password = "", bool staleok=true)
        {
            return Observable.Create<Attachment>(observer =>
            {
                DownloadStringCompletedEventHandler handler = (sender, args) =>
                {
                    if (args.Cancelled)
                        observer.OnCompleted();
                    else if (args.Error != null)
                        observer.OnError(args.Error);
                    else
                    {
                        try
                        {
                            var parsed = JObject.Parse(args.Result);
                            var attachments = parsed.Property("_attachments");
                            if (attachments != null)
                            {
                                foreach (JProperty o in attachments.Value)
                                {
                                    Attachment value = new Attachment();
                                    value.Name = o.Name;                                    
                                    value.ContentType = o.Value["content_type"].ToString();
                                    int l = 0;
                                    if (Int32.TryParse(o.Value["length"].ToString(), out l))
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
                };

                client.DownloadStringCompleted += handler;
                try
                {
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                        client.SetAuthenticationHeaders(username, password);
                    client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                    client.DownloadStringAsync(address);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                return () => client.DownloadStringCompleted -= handler;
            });
        }

        internal static IObservable<Document<T>> DownloadAndConvertAsObservable<T>(this WebClient client, Uri address, string username = "", string password = "", object userToken = null, IScheduler converterScheduler = null)
        {
            return Observable.Create<Document<T>>(observer =>
            {
                DownloadStringCompletedEventHandler handler = (sender, args) =>
                {
                    if (args.UserState != userToken) return;

                    if (args.Cancelled)
                        observer.OnCompleted();
                    else if (args.Error != null)
                        observer.OnError(args.Error);
                    else
                    {
                        try
                        {
                            bool getDoc = false;
                            var parsed = JObject.Parse(args.Result);
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
                                                id = o["doc"]["_id"].ToString();
                                                rev = o["doc"]["_rev"].ToString();
                                            }
                                            else
                                            {
                                                id = o["value"]["_id"].ToString();
                                                rev = o["value"]["_rev"].ToString();
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
                                            id = o["doc"]["_id"].ToString();
                                            rev = o["doc"]["_rev"].ToString();
                                        }
                                        else
                                        {
                                            id = o["value"]["_id"].ToString();
                                            rev = o["value"]["_rev"].ToString();
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
                                            observer.OnNext(new Document<T>(id,parsed.ToObject<T>(),rev));
                                            observer.OnCompleted();
                                        });
                                else
                                {
                                    observer.OnNext(new Document<T>(id, parsed.ToObject<T>(), rev));
                                    observer.OnCompleted();
                                }
                            }
                            
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    }
                };

                client.DownloadStringCompleted += handler;
                try
                {
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                        client.SetAuthenticationHeaders(username, password);
                    client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                    client.DownloadStringAsync(address, userToken);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                return () => client.DownloadStringCompleted -= handler;
            });
        }

        internal static IObservable<Poll<T>> DownloadAndConvertChangesAsObservable<T>(this WebClient client, Uri address, string username = "", string password = "", object userToken = null, IScheduler converterScheduler = null, bool includes_docs = false)
        {
            return Observable.Create<Poll<T>>(observer =>
            {
                DownloadStringCompletedEventHandler handler = (sender, args) =>
                {
                    if (args.UserState != userToken) return;

                    if (args.Cancelled)
                        observer.OnCompleted();
                    else if (args.Error != null)
                        observer.OnError(args.Error);
                    else
                    {
                        try
                        {
                            var parsed = JObject.Parse(args.Result);

                            var results = parsed["results"] as JArray;
                            var seq_id = parsed.Property("last_seq").Value.ToString();
                            var pending = Int32.Parse(parsed.Property("pending").Value.ToString());
                            foreach (JObject result in results)
                            {
                                if (converterScheduler != null)
                                {
                                    converterScheduler.Schedule(() =>
                                    {
                                        string id = string.Empty;
                                        string rev = string.Empty;
                                        string seq = string.Empty;
                                        if (includes_docs)
                                        {
                                            id = result["id"].ToString();
                                            seq = result["seq"].ToString();
                                            var revisions = result["changes"] as JArray;
                                            if (revisions != null && revisions.Count > 0)
                                            {
                                                rev = revisions.Last()["rev"].ToString();
                                            }
                                        }
                                        else
                                        {
                                            id = result["id"].ToString();
                                            seq = result["seq"].ToString();
                                            var revisions = result["changes"] as JArray;
                                            if (revisions != null && revisions.Count > 0)
                                            {
                                                rev = revisions.Last()["rev"].ToString();
                                            }
                                        }
                                        T value = default(T);
                                        value = result.ConvertObject<T>(includes_docs);

                                        observer.OnNext(new Poll<T>{ Document = new Document<T>(id, value, rev), Since=seq});
                                    });
                                }
                                else
                                {
                                    string id = string.Empty;
                                    string rev = string.Empty;
                                    string seq = string.Empty;
                                    if (includes_docs)
                                    {
                                        id = result["id"].ToString();
                                        seq = result["seq"].ToString();
                                        var revisions = result["changes"] as JArray;
                                        if (revisions != null && revisions.Count > 0)
                                        {
                                            rev = revisions.Last()["rev"].ToString();
                                        }
                                    }
                                    else
                                    {
                                        id = result["id"].ToString();
                                        seq = result["seq"].ToString();
                                        var revisions = result["changes"] as JArray;
                                        if (revisions != null && revisions.Count > 0)
                                        {
                                            rev = revisions.Last()["rev"].ToString();
                                        }
                                    }
                                    T value = default(T);
                                    value = result.ConvertObject<T>(includes_docs);
                                    observer.OnNext(new Poll<T> { Document = new Document<T>(id, value, rev) , Since = seq});
                                }
                            }
                            /*
                            }
                            
                            if (results != null)
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
                                                id = o["doc"]["_id"].ToString();
                                                rev = o["doc"]["_rev"].ToString();
                                            }
                                            else
                                            {
                                                id = o["value"]["_id"].ToString();
                                                rev = o["value"]["_rev"].ToString();
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
                                            id = o["doc"]["_id"].ToString();
                                            rev = o["doc"]["_rev"].ToString();
                                        }
                                        else
                                        {
                                            id = o["value"]["_id"].ToString();
                                            rev = o["value"]["_rev"].ToString();
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
                            */
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                        finally
                        {
                            observer.OnCompleted();
                        }
                    }
                };

                client.DownloadStringCompleted += handler;
                try
                {
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                        client.SetAuthenticationHeaders(username, password);
                    client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                    client.DownloadStringAsync(address, userToken);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                return () => client.DownloadStringCompleted -= handler;
            });
        }

        internal static IObservable<string> UploadStringAsObservable(this WebClient client, Uri address, string method, string data, string username = "", string password = "", object userToken = null)
        {
            return Observable.Create<string>(observer =>
            {
                UploadStringCompletedEventHandler handler = (sender, args) =>
                {
                    if (args.UserState != userToken) return;

                    if (args.Cancelled)
                        observer.OnCompleted();
                    else if (args.Error != null)
                        observer.OnError(args.Error);
                    else
                    {
                        try
                        {
                            observer.OnNext(args.Result);
                            observer.OnCompleted();
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }                        
                    }
                };

                client.UploadStringCompleted += handler;
                try
                {
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                        client.SetAuthenticationHeaders(username, password);                    
                    client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                    client.UploadStringAsync(address, method, data, userToken);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                return () => client.UploadStringCompleted -= handler;
            });
        }

        internal static IObservable<byte[]> UploadDataAsObservable(this WebClient client, Uri address, string contentType, string method, byte[] data, string username = "", string password = "", object userToken = null)
        {
            return Observable.Create<byte[]>(observer =>
            {
                UploadDataCompletedEventHandler handler = (sender, args) =>
                {
                    if (args.UserState != userToken) return;

                    if (args.Cancelled)
                        observer.OnCompleted();
                    else if (args.Error != null)
                        observer.OnError(args.Error);
                    else
                    {
                        try
                        {
                            observer.OnNext(args.Result);
                            observer.OnCompleted();
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    }
                };

                client.UploadDataCompleted += handler;
                try
                {
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                        client.SetAuthenticationHeaders(username, password);
                    client.Headers.Add(HttpRequestHeader.ContentType, contentType);
                    client.UploadDataAsync(address, method, data, userToken);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                return () => client.UploadDataCompleted -= handler;
            });
        }

        internal static IObservable<ProgressIndicator> DownloadProgressChangedAsObservable(this WebClient client, object userToken)
        {
            return Observable.FromEventPattern<DownloadProgressChangedEventHandler,DownloadProgressChangedEventArgs>((handler) => client.DownloadProgressChanged += handler,
                                                                                                                     (handler) => client.DownloadProgressChanged -= handler)
                             .Where(x => x.EventArgs.UserState == userToken)
                             .Select(x => new ProgressIndicator(x.EventArgs,userToken.ToString()));
        }

        internal static IObservable<ProgressIndicator> UploadProgressChangedAsObservable(this WebClient client, object userToken)
        {
            return Observable.FromEventPattern<UploadProgressChangedEventHandler, UploadProgressChangedEventArgs>((handler) => client.UploadProgressChanged += handler,
                                                                                                                     (handler) => client.UploadProgressChanged -= handler)
                             .Where(x => x.EventArgs.UserState == userToken)
                             .Select(x => new ProgressIndicator(x.EventArgs, userToken.ToString()));
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
        }

        internal static void SetAuthenticationHeaders(this WebClient client, string username, string password)
        {
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                var authInfo = username + ":" + password;
                client.Headers.Add(HttpRequestHeader.Authorization, "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(authInfo)));                
            }
        }
    }
}
