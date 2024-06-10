using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPLink
{
    internal interface IApiRequest
    {
        public string URL { get; }
        public HttpMethod Method { get; }
        public Dictionary<string, string> RequestHeaders { get; }
        public Dictionary<string, string> ContentHeaders { get; }
        public Dictionary<string, string> QueryParams { get; }
        public object Body { get; }
    }
}
