using System.Collections.Generic;

namespace ReactiveCloudant.Lucene.Interfaces
{
    /// <summary>
    /// An interface for adding parameters to a Lucene Query
    /// </summary>
    public interface ICanAddParameters : ICanAddGroups, ICanAddQuery
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="facet"></param>
        /// <returns></returns>
        ICanAddParameters Facet(bool facet);

        /// <summary>
        /// Indicates whether it is ok to return stale results
        /// </summary>
        /// <param name="staleok"></param>
        /// <returns></returns>
        ICanAddParameters StaleOK(bool staleok);

        /// <summary>
        /// A limit on the results that will be returned
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        ICanAddParameters Limit(int limit);

        /// <summary>
        /// Whether to return the contents of the documents that match the query or not
        /// </summary>
        /// <param name="includeDocs"></param>
        /// <returns></returns>
        ICanAddParameters IncludeDocs(bool includeDocs);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="counts"></param>
        /// <returns></returns>
        ICanAddParameters Counts(IList<string> counts);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        ICanAddParameters Drilldown(string field, string value);

        /// <summary>
        /// Sets the bookmark of the query for continuations
        /// </summary>
        /// <param name="bookmark"></param>
        /// <returns></returns>
        ICanAddParameters Bookmark(string bookmark);

        /// <summary>
        /// Sets the sort of a specific field in the results
        /// </summary>
        /// <param name="field"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        ICanAddParameters Sort(string field, SortOrder order = SortOrder.Ascending);
    }
}
