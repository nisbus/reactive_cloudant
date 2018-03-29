using System.Collections.Generic;

namespace ReactiveCloudant.Lucene
{
    /// <summary>
    /// An untyped Lucene query result
    /// </summary>
    public class LuceneResult
    {
        /// <summary>
        /// The number of rows rom the query
        /// </summary>
        public int TotalRows { get; set; }

        /// <summary>
        /// A bookmark for continuations
        /// </summary>
        public string Bookmark { get; set; }

        /// <summary>
        /// The rows returned in the result
        /// </summary>
        public List<LuceneRow> Rows { get; private set; }

        /// <summary>
        /// Creates a new Lucene result and initalizes the Rows collection
        /// </summary>
        public LuceneResult()
        {
            Rows = new List<LuceneRow>();
        }
    }

    /// <summary>
    /// A typed lucene query result
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LuceneResult<T>
    {
        /// <summary>
        /// The number of rows rom the query
        /// </summary>
        public int TotalRows { get; set; }

        /// <summary>
        /// A bookmark for continuations
        /// </summary>
        public string Bookmark { get; set; }

        /// <summary>
        /// The rows returned in the result
        /// </summary>
        public List<LuceneRow<T>> Rows { get; private set; }

        /// <summary>
        /// Creates a new Lucene result and initalizes the Rows collection
        /// </summary>
        public LuceneResult()
        {
            Rows = new List<LuceneRow<T>>();
        }
    }
}
