using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveCloudant.Lucene.Interfaces
{
    public interface ICanRunQuery
    {
        IObservable<LuceneResult> Execute();
        IObservable<LuceneResult<T>> Execute<T>();     
    }
}
