using System.Collections.Generic;

namespace ReactiveCloudant.Lucene.Interfaces
{
    /// <summary>
    /// An interface to add grouping to a Lucene query
    /// </summary>
    public interface ICanAddGroups : ICanAddQuery
    {
        /// <summary>
        /// A method for setting the field to group by
        /// </summary>
        /// <param name="fieldName">The name of the field to group by</param>
        /// <param name="fieldType">The type of the field</param>
        /// <returns></returns>
        ICanAddGroups Group(string fieldName, string fieldType = "");

        /// <summary>
        /// Adds a limit to the results
        /// </summary>
        /// <param name="limit">The limit to set</param>
        /// <returns></returns>
        ICanAddGroups GroupLimit(int limit);

        /// <summary>
        /// Sets sorting of different fields in the results
        /// </summary>
        /// <param name="groupSortFields">A list of fields to sort by</param>
        /// <returns></returns>
        ICanAddGroups GroupSort(IList<string> groupSortFields);
    }
}
