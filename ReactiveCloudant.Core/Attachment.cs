using System;
using System.Net.Http;

namespace ReactiveCloudant
{
    public class Attachment
    {
        public string Name { get; set; }
        public string ContentType { get; set; }
        public int Length { get; set; }
        public string Digest { get; set; }
        public string Url { get; set; }

        public IObservable<byte[]> Data(string username, string password)
        {
            using (var client = new HttpClient())
            {
                Uri url = new Uri(Url);
                var u = url.AbsoluteUri + Name;
                return client.DownloadAttachment(new Uri(u), ContentType, username, password);
            }
        }
    }
}
