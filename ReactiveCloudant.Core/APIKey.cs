namespace ReactiveCloudant
{
    /// <summary>
    /// An API key for access control 
    /// </summary>
    public class APIKey
    {
        /// <summary>
        /// The username of the key
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// The password of the key
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Creates a new API key
        /// </summary>
        /// <param name="username">the username for the key</param>
        /// <param name="password">the password for the key</param>
        public APIKey(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
