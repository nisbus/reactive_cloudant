using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveCloudant
{
    public class SaveResult
    {
        public string Error { get; private set; }
        public string DocumentId { get; private set; }
        public string RevisionId { get; private set; }
        public bool HasError { get { return !string.IsNullOrWhiteSpace(Error); } }
        public SaveResult(string doc_id, string rev_id)
        {
            DocumentId = doc_id;
            RevisionId = rev_id;
        }
        public SaveResult(string doc_id, string rev_id, string error)
        {
            DocumentId = doc_id;
            RevisionId = rev_id;
            Error = error;
        }

    }
}
