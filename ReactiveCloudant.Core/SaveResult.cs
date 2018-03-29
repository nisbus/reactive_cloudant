namespace ReactiveCloudant
{
    /// <summary>
    /// The result of a save operation
    /// </summary>
    public class SaveResult
    {
        /// <summary>
        /// Contains an error message in case the save operation failed
        /// </summary>
        public string Error { get; private set; }

        /// <summary>
        /// The ID of the document that was saved
        /// </summary>
        public string DocumentId { get; private set; }

        /// <summary>
        /// The revision ID of the document after saving
        /// </summary>
        public string RevisionId { get; private set; }

        /// <summary>
        /// Indicates whether the operation resulted in an error
        /// </summary>
        public bool HasError { get { return !string.IsNullOrWhiteSpace(Error); } }


        /// <summary>
        /// Constructor for creating a save result without error
        /// </summary>
        /// <param name="doc_id">The id of the saved document</param>
        /// <param name="rev_id">The revision id of the saved document</param>
        public SaveResult(string doc_id, string rev_id)
        {
            DocumentId = doc_id;
            RevisionId = rev_id;
        }

        /// <summary>
        /// Constructor for creating a save result with an error message
        /// </summary>
        /// <param name="doc_id">The id of the saved document</param>
        /// <param name="rev_id">The revision id of the saved document</param>
        /// <param name="error">The error message from the operation</param>
        public SaveResult(string doc_id, string rev_id, string error)
        {
            DocumentId = doc_id;
            RevisionId = rev_id;
            Error = error;
        }

    }
}
