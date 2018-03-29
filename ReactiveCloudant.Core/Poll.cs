namespace ReactiveCloudant
{
    /// <summary>
    /// Holder class for polling for changes.
    /// </summary>
    /// <typeparam name="T">The type of document received</typeparam>
    public class Poll<T>
    {
        /// <summary>
        /// The document received
        /// </summary>
        public Document<T> Document { get; set; }

        /// <summary>
        /// The since id for the last document received
        /// </summary>
        public string Since { get; set; }
    }
}
