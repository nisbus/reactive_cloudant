using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveCloudant
{
    public class SaveResult
    {
        public string DocumentId { get; private set; }
        public string RevisionId { get; private set; }
        public SaveResult(string doc_id, string rev_id)
        {
            DocumentId = doc_id;
            RevisionId = rev_id;
        }
    }
}
