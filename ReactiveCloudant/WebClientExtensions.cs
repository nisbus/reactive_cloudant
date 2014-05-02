using System;
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
        internal static IObservable<string> DownloadStringAsObservable(this WebClient client, Uri address, string username = "", string password = "", object userToken = null)
        {
            return Observable.Create<string>(observer =>
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
                        observer.OnNext(args.Result);
                        observer.OnCompleted();
                    }
                };

                client.DownloadStringCompleted += handler;
                
                try
                {
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                    {
                        var authInfo = username + ":" + password;
                        client.Headers.Add(HttpRequestHeader.Authorization, "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(authInfo)));
                        client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                    }
                    client.DownloadStringAsync(address, userToken);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }

                return () => client.DownloadStringCompleted -= handler;
            });
        }

        internal static IObservable<T> DownloadAndConvertAsObservable<T>(this WebClient client, Uri address, string username = "", string password = "", object userToken = null, IScheduler converterScheduler = null)
        {
            return Observable.Create<T>(observer =>
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
                                foreach (JObject o in rows.Value)
                                {
                                    T value = default(T);
                                    if (converterScheduler != null)
                                    {
                                        converterScheduler.Schedule(() =>
                                        {
                                            value = o.ConvertObject<T>(getDoc);
                                            observer.OnNext(value);
                                        });
                                    }
                                    else
                                    {
                                        value = o.ConvertObject<T>(getDoc);
                                        observer.OnNext(value);
                                    }
                                }
                            }
                            else
                            {
                                if (converterScheduler != null)
                                    converterScheduler.Schedule(() => observer.OnNext(parsed.ToObject<T>()));                                        
                                else
                                    observer.OnNext(parsed.ToObject<T>());
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
                    {
                        var authInfo = username + ":" + password;
                        client.Headers.Add(HttpRequestHeader.Authorization, "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(authInfo)));
                        client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                    }
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
                            var response = JObject.Parse(args.Result);
                            if (response == null)
                                observer.OnNext(args.Result);
                            else
                            {
                                var id = response.Property("id");
                                if (id != null)
                                {
                                    JValue val = id.Value as JValue;
                                    var doc_id = id.Value.ToString();
                                    observer.OnNext(doc_id);
                                }

                                var revision = response.Property("rev");
                                if (revision != null)
                                {
                                    JValue val = revision.Value as JValue;
                                    var rev = val.Value.ToString();
                                    observer.OnNext(rev);
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

                client.UploadStringCompleted += handler;
                try
                {
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                    {
                        var authInfo = username + ":" + password;
                        client.Headers.Add(HttpRequestHeader.Authorization, "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(authInfo)));                    
                    }
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
                    return prop.Value.Value<T>();
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
                client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            }
        }
    }
}
