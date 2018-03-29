namespace ReactiveCloudant.Lucene.Interfaces
{
    /// <summary>
    /// Interface for adding a design document to a Lucene Query
    /// </summary>
    public interface ICanAddDesignDoc
    {
        /// <summary>
        /// The design document function
        /// </summary>
        /// <param name="designDoc">The name of the design document</param>
        /// <returns></returns>
        ICanAddIndex DesignDocument(string designDoc);
    }
}
