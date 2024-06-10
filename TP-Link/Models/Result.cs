using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TPLink
{
    internal class Result<T>
    {
        public int StatusCode { get; set; }
        public object Value { get; set; }

        public HttpResponseMessage Response { get; set; }
    }
}
