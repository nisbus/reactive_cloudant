using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveCloudant
{
    public class ProgressIndicator
    {        
        public long Processed { get; private set; }
        public long Remaining { get; private set; }
        public int Percentage { get; private set; }
        public string RequestToken { get; private set; }

        public ProgressIndicator(DownloadProgressChangedEventArgs args,string token = "")
        {
            Processed = args.BytesReceived;
            Remaining = args.TotalBytesToReceive;
            Percentage = args.ProgressPercentage;
            RequestToken = token;
        }

        public ProgressIndicator(UploadProgressChangedEventArgs args, string token = "")
        {
            Processed = args.BytesSent;
            Remaining = args.TotalBytesToSend;
            Percentage = args.ProgressPercentage;
            RequestToken = token;
        }
    }
}
