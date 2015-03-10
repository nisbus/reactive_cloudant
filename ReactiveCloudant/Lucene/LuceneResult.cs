using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveCloudant.Lucene
{
    public class LuceneResult
    {
        public int TotalRows { get; set; }
        public string Bookmark { get; set; }
        public List<LuceneRow> Rows { get; private set; }

        public LuceneResult()
        {
            Rows = new List<LuceneRow>();
        }
    }

    public class LuceneResult<T>
    {
        public int TotalRows { get; set; }
        public string Bookmark { get; set; }
        public List<LuceneRow<T>> Rows { get; private set; }

        public LuceneResult()
        {
            Rows = new List<LuceneRow<T>>();
        }
    }
}
