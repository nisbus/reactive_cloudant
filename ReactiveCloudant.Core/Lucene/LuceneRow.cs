using System.Collections.Generic;

namespace ReactiveCloudant.Lucene
{

    /// <summary>
    /// An untyped lucene query result
    /// </summary>
    public class LuceneRow
    {
        /// <summary>
        /// The id of the query
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The order of the results
        /// </summary>
        public List<double> Order { get; set; }

        /// <summary>
        /// The results of a lucene querue
        /// </summary>
        public Dictionary<string, dynamic> Results { get; private set; }

        /// <summary>
        /// Constructor for a single lucene row
        /// </summary>
        public LuceneRow()
        {
            Results = new Dictionary<string, dynamic>();
            Order = new List<double>();
        }
    }

    /// <summary>
    /// A row of Lucene results of a specific type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LuceneRow<T>
    {
        /// <summary>
        /// The id of the row
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The order of the results
        /// </summary>
        public List<double> Order { get; set; }

        /// <summary>
        /// The results of the query
        /// </summary>
        public List<T> Results { get; private set; }

        /// <summary>
        /// Constructor for a single lucene row
        /// </summary>
        public LuceneRow()
        {
            Results = new List<T>();
            Order = new List<double>();
        }
    }
}
