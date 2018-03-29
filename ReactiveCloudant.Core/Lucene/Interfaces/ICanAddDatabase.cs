namespace ReactiveCloudant.Lucene.Interfaces
{
    /// <summary>
    /// An interface for setting the database for a Lucene query
    /// </summary>
    public interface ICanAddDatabase
    {
        /// <summary>
        /// A method for setting the database of a Lucene query
        /// </summary>
        /// <param name="database">The database name</param>
        /// <returns></returns>
        ICanAddDesignDoc Database(string database);
    }
}
