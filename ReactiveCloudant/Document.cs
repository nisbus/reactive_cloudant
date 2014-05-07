using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveCloudant
{
    public class Document<T>
    {
        public T Item { get; set; }
        public string ID { get; set; }
        public string Version { get; set; }

        public Document(string id, T item, string rev)
        {
            ID = id;
            Item = item;
            Version = rev;
        }
    }
}
