using System;
using System.Net.Http;

namespace ReactiveCloudant
{
    /// <summary>
    /// An attachment can be any binary object stored within a document
    /// </summary>
    public class Attachment
    {
        /// <summary>
        /// The name of the attachment
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The content type of the attachment
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// The length of the attachment
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Digest { get; set; }

        /// <summary>
        /// The url to retrieve the attachment
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The attachment as an Observable byte array
        /// </summary>
        /// <param name="username">The username to access the attachment</param>
        /// <param name="password">The password to access the attachment</param>
        /// <returns></returns>
        public IObservable<byte[]> Data(string username, string password)
        {
            var client = new HttpClient();            
            Uri url = new Uri(Url);
            var u = url.AbsoluteUri + Name;
            var obs = client.DownloadAttachment(new Uri(u), ContentType, username, password);
            obs.Subscribe(_ => { }, () => client.Dispose());
            return obs;
        }
    }
}
