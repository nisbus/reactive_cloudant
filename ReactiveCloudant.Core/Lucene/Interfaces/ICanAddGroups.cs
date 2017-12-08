using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveCloudant.Lucene.Interfaces
{
    public interface ICanAddGroups : ICanAddQuery
    {
        ICanAddGroups Group(string fieldName, string fieldType = "");
        ICanAddGroups GroupLimit(int limit);
        ICanAddGroups GroupSort(IList<string> groupSortFields);
    }
}
