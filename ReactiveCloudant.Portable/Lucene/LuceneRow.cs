using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveCloudant.Lucene
{

    public class LuceneRow
    {
        public string Id { get; set; }
        public List<double> Order { get; set; }
        public Dictionary<string, dynamic> Results { get; private set; }

        public LuceneRow()
        {
            Results = new Dictionary<string, dynamic>();
            Order = new List<double>();
        }
    }

    public class LuceneRow<T>
    {
        public string Id { get; set; }
        public List<double> Order { get; set; }
        public List<T> Results { get; private set; }

        public LuceneRow()
        {
            Results = new List<T>();
            Order = new List<double>();
        }
    }
}
