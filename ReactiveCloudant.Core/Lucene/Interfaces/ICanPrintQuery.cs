namespace ReactiveCloudant.Lucene.Interfaces
{
    /// <summary>
    /// An interface to visualize Lucene Queries as string
    /// </summary>
    public interface ICanPrintQuery
    {
        /// <summary>
        /// Prints the query as a string
        /// </summary>
        /// <returns>The query as a string</returns>
        string ShowQuery();
    }
}
