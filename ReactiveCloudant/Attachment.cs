using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
            using (var client = new WebClient())
            {
                Uri url = new Uri(Url);
                var u = url.AbsoluteUri + Name;
                return client.DownloadAttachment(new Uri(u), ContentType, username, password);
            }
        }
    }
}
