using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveCloudant.Lucene.Interfaces;
using System.Net.Http;

namespace ReactiveCloudant.Lucene
{
    public enum SortOrder
    {
        Ascending,
        Descending
    }

    /// <summary>
    /// Construct a lucene query against a Cloudant Search index.
    /// Uses a fluent interface to set parameters and construct the query.
    /// </summary>
    /// <example>
    /// public void Test()
    /// {
    ///     public static void LuceneTest()
    ///     {
    ///         var session = new CloudantSession(url, username, password);
    ///         LuceneQuery.Session(session)
    ///                    .Database("myDatabase")
    ///                    .DesignDocument("mySearchDesignDoc")
    ///                    .Index("mySearchIndex")
    ///                    .StaleOK(true)
    ///                    .IncludeDocs(true)
    ///                    .Fuzzy("myField","fuzzymatch")
    ///                    .And("docType","sampleDoc")
    ///                    .Execute<MyResultType>()
    ///                    .Subscribe(results =>
    ///                    {
    ///                         results.
    ///                    });
    ///     }
    /// }
    /// </example>
    /// 
    public static class TEST
    {
        /*
        public static void LuceneTest()
         {
             var session = new CloudantSession(url, username, password);
             LuceneQuery.Session(session)
                        .Database("myDatabase")
                        .DesignDocument("mySearchDesignDoc")
                        .Index("mySearchIndex")
                        .StaleOK(true)
                        .IncludeDocs(true)
                        .Fuzzy("myField","fuzzymatch")
                        .And("docType","sampleDoc")
                        .Execute<MyResultType>()
                        .Subscribe(results =>
                        {
                             
                        });
         }
         */
    }
    
    public class LuceneQuery : ICanAddDatabase, ICanAddDesignDoc, ICanAddIndex,ICanAddGroups, ICanAddParameters, ICanAddQuery, ICanRunQuery, ICanPrintQuery
    {
        #region fields

        CloudantSession session;
        string db;
        string designDoc;
        string index;
        bool facet;
        string bookmark;
        List<string> counts = new List<string>();
        Dictionary<string, string> drilldown = new Dictionary<string, string>();
        List<string> sorts = new List<string>();
        bool includeDocs;
        int limit;
        bool staleok;
        string group;
        int grouplimit;
        List<string> groupSorts = new List<string>();        
        string query = string.Empty;

        #endregion

        #region CTOR

        private LuceneQuery(CloudantSession session)
        {
            this.session = session;           
        }

        #endregion

        #region Init

        public static ICanAddDatabase Session(CloudantSession session)
        {
            return new LuceneQuery(session);
        }

        public ICanAddDesignDoc Database(string database)
        {
            db = database;
            return this;
        }

        public ICanAddIndex DesignDocument(string designDoc)
        {
            if (designDoc.StartsWith("_design"))
                this.designDoc = designDoc;
            else
                this.designDoc = "_design/" + designDoc;
            return this;
        }

        public ICanAddParameters Index(string indexName)
        {
            this.index = indexName;
            return this;
        }

        #endregion

        #region Parameters

        public ICanAddParameters Bookmark(string bookmark)
        {
            this.bookmark = bookmark;
            return this;
        }

        public ICanAddParameters Facet(bool facet)
        {
            this.facet = facet;
            return this;
        }

        public ICanAddParameters StaleOK(bool staleok)
        {
            this.staleok = staleok;
            return this;
        }

        public ICanAddParameters Limit(int limit)
        {
            this.limit = limit;
            return this;
        }

        public ICanAddParameters IncludeDocs(bool includeDocs)
        {
            this.includeDocs = includeDocs;
            return this;
        }

        public ICanAddParameters Counts(IList<string> counts)
        {
            if(counts != null)
                foreach(var count in counts)
                this.counts.Add(count);
            return this;
        }

        public ICanAddParameters Drilldown(string field, string value)
        {
            this.drilldown.Add(field, value);
            return this;
        }

        public ICanAddParameters Sort(string field, SortOrder order = SortOrder.Ascending)
        {
            if (order == SortOrder.Ascending)
                this.sorts.Add(field);
            else
                this.sorts.Add("-" + field);            
            return this;
        }

        #endregion

        #region Grouping

        public ICanAddGroups Group(string fieldName, string fieldType = "")
        {
            this.group = string.IsNullOrEmpty(fieldName) ? fieldName + "<string>" : fieldName + "<" + fieldType + ">";
            return this;
        }

        public ICanAddGroups GroupLimit(int limit)
        {
            this.grouplimit = limit;
            return this;
        }

        public ICanAddGroups GroupSort(IList<string> groupSortFields)
        {
            if (groupSortFields != null)
            {
                foreach (var s in groupSortFields)
                {
                    this.groupSorts.Add(s);
                }
            }
            return this;
        }


        #endregion

        #region Query

        private static List<string> needEscaping = new List<string> { "+", "-", "&&", "||", "!", "(", ")", "{", "}", "[", "]", "^", "\"", "~", "*", "?", ":", "/" };
        private static string Escape(string value)
        {
            value = value.Replace("\\", "\\\\");
            foreach (var e in needEscaping)
                value = value.Replace(e, "\\" + e);
            value = value.Replace(" ", "%c5%20");
            return value;
        }
        private static string AddToQuery(string field, string condition, string operation, string query)
        {

            var qstring = operation + " " + Escape(field) + ":" + condition;
            if (string.IsNullOrWhiteSpace(query))
                return Escape(field) + ":" + condition;
            else
                return query + " " + qstring;
        }

        public ICanAddQuery And(string field, string condition)
        {
            
            query = AddToQuery(field, condition, "AND", query);
            return this;
        }

        public ICanAddQuery Or(string field, string condition)
        {
            query = AddToQuery(field, condition, "OR", query);
            return this;
        }

        public ICanAddQuery Not(string field, string condition)
        {
            query = AddToQuery(field, condition, "NOT", query);
            return this;
        }

        public ICanAddQuery Plus(string field, string condition)
        {
            query = AddToQuery(field, condition, "+", query);
            return this;
        }

        public ICanAddQuery Minus(string field, string condition)
        {
            query = AddToQuery(field, condition, "-", query);
            return this;
        }

        public ICanAddQuery Fuzzy(string field, string condition)
        {
            var fuzz = field+":"+condition+"~";
            if (!string.IsNullOrWhiteSpace(query))
                query += " " + fuzz;
            else
                query += fuzz;
            return this;
        }

        public ICanAddQuery Range(string field, string lowerRange, string upperRange)
        {
            string range = string.Empty;
            if (!string.IsNullOrWhiteSpace(query))
                range += " ";
            range = field + ":[" + lowerRange + " TO " + upperRange + "]";
            query += range;
            return this;
        }

        public ICanAddQuery StartsWith(string field, string startString)
        {            
            if (!string.IsNullOrWhiteSpace(query))
                query += " ";
            
            query += field+":"+startString+"*";
            return this;
        }

        public ICanAddQuery StartsWithConstrained(string field, string startString)
        {
            if (!string.IsNullOrWhiteSpace(query))
                query += " ";

            query += field + ":" + startString + "?";
            return this;
        }

        #endregion

        #region Execute queries

        public IObservable<LuceneResult<T>> Execute<T>()
        {
            return Observable.Create<LuceneResult<T>>(observer =>
            {
                string url = session.BaseUrl + db + "/" + designDoc + "/_search/" + index+"?q="+query+BuildOptions();
                Func<string, LuceneResult<T>> conv = (json) =>
                {
                    LuceneResult<T> retVal = new LuceneResult<T>();
                    var j = JObject.Parse(json);
                    retVal.TotalRows = j.Value<int>("total_rows");
                    retVal.Bookmark = j.Value<string>("bookmark");
                    var rows = j.Value<JArray>("rows");
                    foreach (var row in rows)
                    {
                        LuceneRow<T> res = new LuceneRow<T>();
                        res.Id = row.Value<string>("id");
                        var order = ((JArray)row["order"]);
                        foreach(var o in order)
                            res.Order.Add(o.ToObject<double>());
                        if (!includeDocs)
                        {
                            var fields = row["fields"];
                            foreach (JProperty field in fields)
                            {
                                res.Results.Add(field.ToObject<T>());
                            }
                        }
                        else
                        {    
                            res.Results.Add(row["doc"].ToObject<T>());
                        }
                        retVal.Rows.Add(res);
                    }
                    return retVal;
                };
                //var query = BuildCloudantQuery(selector, returnFields, limit: limit, sorting: sorting, skip: skip, readQuorum: readQuorum);
                using (HttpClient client = new HttpClient())
                {
                    client.DownloadStringAsObservable(new Uri(url), session.Username, session.Password).Subscribe(result =>
                    {
                        try
                        {
                            observer.OnNext(conv.Invoke(result));
                        }
                        catch (Exception error)
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

        public IObservable<LuceneResult> Execute()
        {
            return Observable.Create<LuceneResult>(observer =>
            {
                string url = session.BaseUrl + db + "/" + designDoc + "/_search/" + index + "?q=" + query+ BuildOptions();
                Func<string, LuceneResult> conv = (json) =>
                {
                    LuceneResult retVal = new LuceneResult();
                    var j = JObject.Parse(json);
                    retVal.TotalRows = j.Value<int>("total_rows");
                    retVal.Bookmark = j.Value<string>("bookmark");
                    var rows = j.Value<JArray>("rows");
                    foreach (var row in rows)
                    {
                        LuceneRow res = new LuceneRow();
                        res.Id = row.Value<string>("id");
                        var order = ((JArray)row["order"]);
                        foreach(var o in order)
                            res.Order.Add(o.ToObject<double>());
                        if (!includeDocs)
                        {
                            var fields = row["fields"];
                            foreach (JProperty field in fields)
                            {                                
                                res.Results.Add(field.Name, field.Value);
                            }
                        }
                        else
                        {
                            
                            JObject doc = row["doc"] as JObject;
                            foreach (var prop in doc.Properties())
                            {
                                res.Results.Add(prop.Name, prop.Value);
                            }
                        }
                        retVal.Rows.Add(res);
                    }
                    return retVal;
                };
                //var query = BuildCloudantQuery(selector, returnFields, limit: limit, sorting: sorting, skip: skip, readQuorum: readQuorum);
                using (HttpClient client = new HttpClient())
                {
                    client.DownloadStringAsObservable(new Uri(url), session.Username, session.Password).Subscribe(result =>
                    {
                        try
                        {
                            observer.OnNext(conv.Invoke(result));
                        }
                        catch (Exception error)
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

        public IObservable<LuceneResult<T>> Execute<T>(string query)
        {
            return Observable.Create<LuceneResult<T>>(observer =>
            {
                string url = session.BaseUrl + db + "/" + designDoc + "/_search/" + index + "?q=" + query + BuildOptions();
                Func<string, LuceneResult<T>> conv = (json) =>
                {
                    LuceneResult<T> retVal = new LuceneResult<T>();
                    var j = JObject.Parse(json);
                    retVal.TotalRows = j.Value<int>("total_rows");
                    retVal.Bookmark = j.Value<string>("bookmark");
                    var rows = j.Value<JArray>("rows");
                    foreach (var row in rows)
                    {
                        LuceneRow<T> res = new LuceneRow<T>();
                        res.Id = row.Value<string>("id");
                        var order = ((JArray)row["order"]);
                        foreach (var o in order)
                            res.Order.Add(o.ToObject<double>());
                        if (!includeDocs)
                        {
                            var fields = row["fields"];
                            foreach (JProperty field in fields)
                            {
                                res.Results.Add(field.ToObject<T>());
                            }
                        }
                        else
                        {
                            res.Results.Add(row["doc"].ToObject<T>());
                        }
                        retVal.Rows.Add(res);
                    }
                    return retVal;
                };
                //var query = BuildCloudantQuery(selector, returnFields, limit: limit, sorting: sorting, skip: skip, readQuorum: readQuorum);
                using (HttpClient client = new HttpClient())
                {
                    client.DownloadStringAsObservable(new Uri(url), session.Username, session.Password).Subscribe(result =>
                    {
                        try
                        {
                            observer.OnNext(conv.Invoke(result));
                        }
                        catch (Exception error)
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

        public IObservable<LuceneResult> Execute(string query)
        {
            return Observable.Create<LuceneResult>(observer =>
            {
                string url = session.BaseUrl + db + "/" + designDoc + "/_search/" + index + "?q=" + query + BuildOptions();
                Func<string, LuceneResult> conv = (json) =>
                {
                    LuceneResult retVal = new LuceneResult();
                    var j = JObject.Parse(json);
                    retVal.TotalRows = j.Value<int>("total_rows");
                    retVal.Bookmark = j.Value<string>("bookmark");
                    var rows = j.Value<JArray>("rows");
                    foreach (var row in rows)
                    {
                        LuceneRow res = new LuceneRow();
                        res.Id = row.Value<string>("id");
                        var order = ((JArray)row["order"]);
                        foreach (var o in order)
                            res.Order.Add(o.ToObject<double>());
                        if (!includeDocs)
                        {
                            var fields = row["fields"];
                            foreach (JProperty field in fields)
                            {
                                res.Results.Add(field.Name, field.Value);
                            }
                        }
                        else
                        {

                            JObject doc = row["doc"] as JObject;
                            foreach (var prop in doc.Properties())
                            {
                                res.Results.Add(prop.Name, prop.Value);
                            }
                        }
                        retVal.Rows.Add(res);
                    }
                    return retVal;
                };
                //var query = BuildCloudantQuery(selector, returnFields, limit: limit, sorting: sorting, skip: skip, readQuorum: readQuorum);
                using (HttpClient client = new HttpClient())
                {
                    client.DownloadStringAsObservable(new Uri(url), session.Username, session.Password).Subscribe(result =>
                    {
                        try
                        {
                            observer.OnNext(conv.Invoke(result));
                        }
                        catch (Exception error)
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

        private string BuildOptions()
        {
            if(!string.IsNullOrWhiteSpace(bookmark) && staleok)
                throw new ArgumentException("Do not combine the bookmark and stale options. \rn"+
                                            "The reason is that both these options constrain the choice of shard replicas to use for determining the response. \rn"+
                                            "When used together, the options can result in problems when attempting to contact slow or unavailable replicas.");
            string ret = string.Empty;
            List<string> options = new List<string>();
            if(facet)
                options.Add("facet=true");
            if(!string.IsNullOrWhiteSpace(bookmark))
                options.Add("bookmark="+bookmark);
            if (limit > 0)
                options.Add("limit=" + limit.ToString());
            if (includeDocs)
                options.Add("include_docs=true");
            if (counts != null && counts.Count > 0)
                options.Add("counts=["+string.Join(",",counts)+"]");
            if (drilldown != null && drilldown.Count > 0)
                foreach (var drill in drilldown)
                    options.Add("drilldown=[" + drill.Key + "," + drill.Value + "]");
            if (sorts != null && sorts.Count > 0)
            {
                if (sorts.Count == 1)
                    options.Add("sorts=" + sorts[0]);
                else
                {
                    options.Add("sorts=[" + string.Join(",", sorts) + "]");
                }
            }
            if (!string.IsNullOrWhiteSpace(group))
                options.Add("group_field=" + group);
            if (grouplimit > 0)
                options.Add("group_limit=" + grouplimit);
            if (groupSorts != null && groupSorts.Count > 0)
            {
                if (sorts.Count == 1)
                    options.Add("group_sort=" + groupSorts[0]);
                else
                {
                    options.Add("group_sort=[" + string.Join(",", groupSorts) + "]");
                }
            }
            if (options.Count > 0)
                return "&" + string.Join("&", options);
            else return string.Empty;
        }

        #region ICanPrintQuery

        public string ShowQuery()
        {
            return query;
        }

        #endregion
    }    
}
