namespace ReactiveCloudant
{
    /// <summary>
    /// A typed document
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Document<T>
    {
        /// <summary>
        /// The document contents 
        /// </summary>
        public T Item { get; set; }

        /// <summary>
        /// The id of the document
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// The version/revisionID of the document
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Creates a new Document
        /// </summary>
        /// <param name="id">The id of the document</param>
        /// <param name="item">The contents of the document</param>
        /// <param name="rev">The revision id of the document</param>
        public Document(string id, T item, string rev)
        {
            ID = id;
            Item = item;
            Version = rev;
        }
    }
}
