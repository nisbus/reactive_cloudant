using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using ReactiveCloudant.Lucene.Interfaces;
using System.Net.Http;

namespace ReactiveCloudant.Lucene
{
    /// <summary>
    /// The sort order for a lucene query
    /// </summary>
    public enum SortOrder
    {
        /// <summary>
        /// Ascending order
        /// </summary>
        Ascending,
        /// <summary>
        /// Descending order
        /// </summary>
        Descending
    }
    
    /// <summary>
    /// A lucene querye to the cloudant datastore
    /// </summary>
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
        string _Query = string.Empty;

        #endregion

        #region CTOR

        /// <summary>
        /// Creates a new LuceneQuery
        /// </summary>
        /// <param name="session">The session to perform the query on</param>
        private LuceneQuery(CloudantSession session)
        {
            this.session = session;           
        }

        #endregion

        #region Init

        /// <summary>
        /// Creates a new LuceneQuery with the session to query on
        /// </summary>
        /// <param name="session">The session to perform the query on</param>
        /// <returns></returns>
        public static ICanAddDatabase Session(CloudantSession session)
        {
            return new LuceneQuery(session);
        }

        /// <summary>
        /// Sets database to query
        /// </summary>
        /// <param name="database">The name of the database</param>
        /// <returns></returns>
        public ICanAddDesignDoc Database(string database)
        {
            db = database;
            return this;
        }

        /// <summary>
        /// Sets the design document to query
        /// </summary>
        /// <param name="designDoc">The name of the design document</param>
        /// <returns></returns>
        public ICanAddIndex DesignDocument(string designDoc)
        {
            if (designDoc.StartsWith("_design"))
                this.designDoc = designDoc;
            else
                this.designDoc = "_design/" + designDoc;
            return this;
        }

        /// <summary>
        /// Sets the index to query
        /// </summary>
        /// <param name="indexName">The name of the index to use</param>
        /// <returns></returns>
        public ICanAddParameters Index(string indexName)
        {
            this.index = indexName;
            return this;
        }

        #endregion

        #region Parameters

        /// <summary>
        /// Sets a bookmark on the query
        /// </summary>
        /// <param name="bookmark">The bookmark to set</param>
        /// <returns></returns>
        public ICanAddParameters Bookmark(string bookmark)
        {
            this.bookmark = bookmark;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="facet"></param>
        /// <returns></returns>
        public ICanAddParameters Facet(bool facet)
        {
            this.facet = facet;
            return this;
        }

        /// <summary>
        /// Sets the stale OK parameter for the query
        /// </summary>
        /// <param name="staleok">True or False whether to return stale documents or not</param>
        /// <returns></returns>
        public ICanAddParameters StaleOK(bool staleok)
        {
            this.staleok = staleok;
            return this;
        }

        /// <summary>
        /// Sets a limit on the returned rows
        /// </summary>
        /// <param name="limit">The row limit to set</param>
        /// <returns></returns>
        public ICanAddParameters Limit(int limit)
        {
            this.limit = limit;
            return this;
        }

        /// <summary>
        /// If true the results will include the documents that belong to the results
        /// </summary>
        /// <param name="includeDocs">True or False whether to inclued documents in results</param>
        /// <returns></returns>
        public ICanAddParameters IncludeDocs(bool includeDocs)
        {
            this.includeDocs = includeDocs;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="counts"></param>
        /// <returns></returns>
        public ICanAddParameters Counts(IList<string> counts)
        {
            if(counts != null)
                foreach(var count in counts)
                this.counts.Add(count);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public ICanAddParameters Drilldown(string field, string value)
        {
            this.drilldown.Add(field, value);
            return this;
        }

        /// <summary>
        /// Sets the sorting of the results by a certain field
        /// </summary>
        /// <param name="field">The field to sort by</param>
        /// <param name="order">The order to sort in (defaults to Ascending)</param>
        /// <returns></returns>
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

        /// <summary>
        /// Sets grouping on the results by field
        /// </summary>
        /// <param name="fieldName">The field to group by</param>
        /// <param name="fieldType">The type of the field values (defaults to string)</param>
        /// <returns></returns>
        public ICanAddGroups Group(string fieldName, string fieldType = "")
        {
            this.group = string.IsNullOrEmpty(fieldName) ? fieldName + "<string>" : fieldName + "<" + fieldType + ">";
            return this;
        }

        /// <summary>
        /// Adds a limit to grouping
        /// </summary>
        /// <param name="limit">The limit for the returned results</param>
        /// <returns></returns>
        public ICanAddGroups GroupLimit(int limit)
        {
            this.grouplimit = limit;
            return this;
        }

        /// <summary>
        /// Adds a sort to the group fields
        /// </summary>
        /// <param name="groupSortFields">The fields to sort by</param>
        /// <returns></returns>
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

        /// <summary>
        /// An AND constraint to a query
        /// </summary>
        /// <param name="field">The field to apply the AND constraint to</param>
        /// <param name="condition">The condition for the match</param>
        /// <returns></returns>
        public ICanAddQuery And(string field, string condition)
        {
            
            _Query = AddToQuery(field, condition, "AND", _Query);
            return this;
        }

        /// <summary>
        /// An OR constraint to a query
        /// </summary>
        /// <param name="field">The field to apply the OR constraint to</param>
        /// <param name="condition">The condition for the match</param>
        /// <returns></returns>
        public ICanAddQuery Or(string field, string condition)
        {
            _Query = AddToQuery(field, condition, "OR", _Query);
            return this;
        }

        /// <summary>
        /// A NOT constraint to a query
        /// </summary>
        /// <param name="field">The field to apply the NOT constraint to</param>
        /// <param name="condition">The condition for the match</param>
        /// <returns></returns>
        public ICanAddQuery Not(string field, string condition)
        {
            _Query = AddToQuery(field, condition, "NOT", _Query);
            return this;
        }

        /// <summary>
        /// A PLUS constraint to a query
        /// </summary>
        /// <param name="field">The field to apply the PLUS constraint to</param>
        /// <param name="condition">The condition for the match</param>
        /// <returns></returns>
        public ICanAddQuery Plus(string field, string condition)
        {
            _Query = AddToQuery(field, condition, "+", _Query);
            return this;
        }

        /// <summary>
        /// A MINUS constraint to a query
        /// </summary>
        /// <param name="field">The field to apply the MINUS constraint to</param>
        /// <param name="condition">The condition for the match</param>
        /// <returns></returns>
        public ICanAddQuery Minus(string field, string condition)
        {
            _Query = AddToQuery(field, condition, "-", _Query);
            return this;
        }

        /// <summary>
        /// Sets a search constraint with fuzzy match
        /// </summary>
        /// <param name="field">The field to query</param>
        /// <param name="condition">The string to search</param>
        /// <returns></returns>
        public ICanAddQuery Fuzzy(string field, string condition)
        {
            var fuzz = field+":"+condition+"~";
            if (!string.IsNullOrWhiteSpace(_Query))
                _Query += " " + fuzz;
            else
                _Query += fuzz;
            return this;
        }

        /// <summary>
        /// A range query
        /// </summary>
        /// <param name="field">The field to query</param>
        /// <param name="lowerRange">The lower range of the query search</param>
        /// <param name="upperRange">The upper range of the query search</param>
        /// <returns></returns>
        public ICanAddQuery Range(string field, string lowerRange, string upperRange)
        {
            string range = string.Empty;
            if (!string.IsNullOrWhiteSpace(_Query))
                range += " ";
            range = field + ":[" + lowerRange + " TO " + upperRange + "]";
            _Query += range;
            return this;
        }

        /// <summary>
        /// Sets a search constraint for starts with string
        /// </summary>
        /// <param name="field">The field to query</param>
        /// <param name="startString">The string to search for with a starts with query</param>
        /// <returns></returns>
        public ICanAddQuery StartsWith(string field, string startString)
        {            
            if (!string.IsNullOrWhiteSpace(_Query))
                _Query += " ";
            
            _Query += field+":"+startString+"*";
            return this;
        }

        /// <summary>
        /// Sets a search constraint for starts with string
        /// </summary>
        /// <param name="field">The field to query</param>
        /// <param name="startString">The string to search for with a starts with query</param>
        /// <returns></returns>
        public ICanAddQuery StartsWithConstrained(string field, string startString)
        {
            if (!string.IsNullOrWhiteSpace(_Query))
                _Query += " ";

            _Query += field + ":" + startString + "?";
            return this;
        }

        #endregion

        #region Execute queries

        /// <summary>
        /// Executes the query
        /// </summary>
        /// <typeparam name="T">They type of each result</typeparam>
        /// <returns>An Observable LuceneResult of type T</returns>
        public IObservable<LuceneResult<T>> Execute<T>()
        {
            return Observable.Create<LuceneResult<T>>(observer =>
            {
                string url = session.BaseUrl + db + "/" + designDoc + "/_search/" + index+"?q="+_Query+BuildOptions();
                LuceneResult<T> conv(string json)
                {
                    LuceneResult<T> retVal = new LuceneResult<T>();
                    var j = JObject.Parse(json);
                    retVal.TotalRows = j.Value<int>("total_rows");
                    retVal.Bookmark = j.Value<string>("bookmark");
                    var rows = j.Value<JArray>("rows");
                    foreach (var row in rows)
                    {
                        LuceneRow<T> res = new LuceneRow<T>
                        {
                            Id = row.Value<string>("id")
                        };
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
                }
                //var query = BuildCloudantQuery(selector, returnFields, limit: limit, sorting: sorting, skip: skip, readQuorum: readQuorum);
                using (HttpClient client = new HttpClient())
                {
                    client.DownloadStringAsObservable(new Uri(url), session.Username, session.Password).Subscribe(result =>
                    {
                        try
                        {
                            observer.OnNext(conv(result));
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

        /// <summary>
        /// Executes a non typed Lucene query
        /// </summary>
        /// <returns>An Observable <see cref="LuceneResult"/></returns>
        public IObservable<LuceneResult> Execute()
        {
            return Observable.Create<LuceneResult>(observer =>
            {
                string url = session.BaseUrl + db + "/" + designDoc + "/_search/" + index + "?q=" + _Query+ BuildOptions();
                LuceneResult conv(string json)
                {
                    LuceneResult retVal = new LuceneResult();
                    var j = JObject.Parse(json);
                    retVal.TotalRows = j.Value<int>("total_rows");
                    retVal.Bookmark = j.Value<string>("bookmark");
                    var rows = j.Value<JArray>("rows");
                    foreach (var row in rows)
                    {
                        LuceneRow res = new LuceneRow
                        {
                            Id = row.Value<string>("id")
                        };
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
                }
                //var query = BuildCloudantQuery(selector, returnFields, limit: limit, sorting: sorting, skip: skip, readQuorum: readQuorum);
                using (HttpClient client = new HttpClient())
                {
                    client.DownloadStringAsObservable(new Uri(url), session.Username, session.Password).Subscribe(result =>
                    {
                        try
                        {
                            observer.OnNext(conv(result));
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

        /// <summary>
        /// Same as Execute[T] only with a string query supplied by the user
        /// </summary>
        /// <param name="query">The query to execute</param>
        /// <returns></returns>
        public IObservable<LuceneResult<T>> Execute<T>(string query)
        {
            _Query = query;
            return Execute<T>();            
        }

        /// <summary>
        /// Same as Execute only with a string query supplied by the user
        /// </summary>
        /// <param name="query">The query to execute</param>
        /// <returns></returns>
        public IObservable<LuceneResult> Execute(string query)
        {
            _Query = query;
            return Execute();
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

        /// <summary>
        /// Shows the query to be executed on Execute
        /// </summary>
        /// <returns></returns>
        public string ShowQuery()
        {
            return _Query;
        }

        #endregion
    }    
}
