namespace ReactiveCloudant.Lucene.Interfaces
{
    /// <summary>
    /// An interface to add indexes to Lucene querys
    /// </summary>
    public interface ICanAddIndex
    {
        /// <summary>
        /// A method to set the index name of a query
        /// </summary>
        /// <param name="indexName">The name of the index to use</param>
        /// <returns></returns>
        ICanAddParameters Index(string indexName);
    }
}
