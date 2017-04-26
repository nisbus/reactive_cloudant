using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveCloudant.Lucene.Interfaces
{
    public interface ICanAddParameters : ICanAddGroups, ICanAddQuery
    {
        ICanAddParameters Facet(bool facet);
        ICanAddParameters StaleOK(bool staleok);
        ICanAddParameters Limit(int limit);
        ICanAddParameters IncludeDocs(bool includeDocs);
        ICanAddParameters Counts(IList<string> counts);
        ICanAddParameters Drilldown(string field, string value);
        ICanAddParameters Bookmark(string bookmark);
        ICanAddParameters Sort(string field, SortOrder order = SortOrder.Ascending);
    }
}
