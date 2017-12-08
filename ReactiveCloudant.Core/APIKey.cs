using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveCloudant
{
    public class APIKey
    {
        public string Username { get; private set; }
        public string Password { get; private set; }
        public APIKey(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
