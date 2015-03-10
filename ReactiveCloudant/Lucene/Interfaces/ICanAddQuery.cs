using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveCloudant.Lucene.Interfaces
{
    public interface ICanAddQuery : ICanRunQuery
    {
        ICanAddQuery And(string field, string condition);
        ICanAddQuery Or(string field, string condition);
        ICanAddQuery Not(string field, string condition);
        ICanAddQuery Plus(string field, string condition);
        ICanAddQuery Minus(string field, string condition);
        ICanAddQuery Fuzzy(string field, string condition);
        ICanAddQuery Range(string field, string lowerRange, string upperRange);
        ICanAddQuery StartsWith(string field, string startString);
        ICanAddQuery StartsWithConstrained(string field, string startString);
    }
}
