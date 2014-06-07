using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactiveCloudant
{
    public class Poll<T>
    {
        public Document<T> Document { get; set; }
        public string Since { get; set; }
    }
}
