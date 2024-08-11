using System.Net;
using System.Text;
using System.Web;
using System.Xml.Serialization;

namespace FrApp42.TPLink
{
    internal class ApiRequest : IApiRequest
    {

        #region Private Variables

        private HttpClientHandler _httpClientHandler;
        private HttpClient _httpClient;

        #endregion

        #region Public Variables

        public string URL { get; private set; } = string.Empty;
        public HttpMethod Method { get; private set; } = HttpMethod.Get;

        public Dictionary<string, string> RequestHeaders { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string> ContentHeaders { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string> QueryParams { get; private set; } = new Dictionary<string, string>();

        public object Body { get; private set; } = null;

        #endregion

        #region Constructors

        public ApiRequest()
        {
            _httpClient = new HttpClient();
        }
        public ApiRequest(CookieContainer cookies)
        {
            _httpClientHandler = new HttpClientHandler();
            _httpClientHandler.UseCookies = true;
            _httpClientHandler.CookieContainer = cookies;

            _httpClient = new HttpClient(_httpClientHandler);
        }

        public ApiRequest(string url) : this()
        {
            URL = url;
#if DEBUG
            Console.WriteLine($"ApiRequest URL : {url}");
#endif
        }

        public ApiRequest(string url, CookieContainer cookies) : this(cookies)
        {
            URL = url;
#if DEBUG
            Console.WriteLine($"ApiRequest URL : {url}");
#endif
        }

        public ApiRequest(string url, HttpMethod httpMethod) : this(url)
        {
            Method = httpMethod;
        }

        public ApiRequest(string url, HttpMethod httpMethod, CookieContainer cookies) : this(url, cookies)
        {
            Method = httpMethod;
        }

        #endregion

        #region Public Functions
        public ApiRequest AddHeader(string key, string value)
        {
            RequestHeaders.Add(key, value);
            return this;
        }

        public ApiRequest AddHeaders(Dictionary<string, string> headers)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                RequestHeaders.Add(header.Key, header.Value);
            }
            return this;
        }

        public ApiRequest AddContentHeader(string key, string value)
        {
            ContentHeaders.Add(key, value);
            return this;
        }

        public ApiRequest AddContentHeaders(Dictionary<string, string> headers)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                ContentHeaders.Add(header.Key, header.Value);
            }
            return this;
        }

        public ApiRequest AddQueryParam(string key, string value)
        {
            QueryParams.Add(key, value);
#if DEBUG
            Console.WriteLine($"ApiRequest Add Query params : {key}={value}");
#endif
            return this;
        }

        public ApiRequest AddQueryParams(Dictionary<string, string> queryParams)
        {
            foreach (KeyValuePair<string, string> query in queryParams)
            {
                QueryParams.Add(query.Key, query.Value);
#if DEBUG
                Console.WriteLine($"ApiRequest Add Query params : {query.Key}={query.Value}");
#endif
            }
            return this;
        }

        public ApiRequest AddBody(string body)
        {
            Body = body;
            return this;
        }

        public async Task<Result<T>> Run<T>()
        {
            string url = BuildUrl();

#if DEBUG
            Console.WriteLine($"ApiRequest full URL : {url}");
#endif
            HttpRequestMessage request = new HttpRequestMessage(Method, url);
            for (int i = 0; i < RequestHeaders.Count; i++)
            {
                KeyValuePair<string, string> header = RequestHeaders.ElementAt(i);
                request.Headers.Add(header.Key, header.Value);
            }

            if (Body != null)
            {
                request.Content = new StringContent((string)Body, Encoding.UTF8, "text/plain");
                //request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                
            }

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            Result<T> result = new Result<T>
            {
                StatusCode = (int)response.StatusCode,
                Response = response
            };

            if (response.IsSuccessStatusCode)
            {
                string MediaType = response.Content?.Headers?.ContentType?.MediaType.ToLower();
                string ContentResponse = await response.Content?.ReadAsStringAsync();

                switch (true)
                {
                    case bool b when (MediaType.Contains("application/xml")):
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                        StringReader reader = new StringReader(ContentResponse);
                        result.Value = (T)xmlSerializer.Deserialize(reader);
                        break;
                    default:
                        result.Value = ContentResponse;
                        break;
                }
            }
            else
            {
                throw new Exception(response.StatusCode.ToString());
            }
            return result;
        }

        #endregion

        #region Private Functions

        private string BuildUrl()
        {
            StringBuilder builder = new(URL);
            string fullUrl = URL;

            if (fullUrl.EndsWith("/"))
                builder.Remove(fullUrl.Length - 1, 1);

            if (QueryParams.Count() > 0)
            {
                builder.Append("?");

                for (int i = 0; i < QueryParams.Count(); i++)
                {
                    KeyValuePair<string, string> query = QueryParams.ElementAt(i);
                    builder.Append($"{query.Key}={HttpUtility.UrlEncode(query.Value)}");

                    if (!(i == QueryParams.Count() - 1))
                        builder.Append("&");
                }
            }

            return builder.ToString();
        }

        private HttpRequestMessage BuildBaseRequest()
        {
            HttpRequestMessage request = new(Method, BuildUrl());

            for (int i = 0; i < RequestHeaders.Count(); i++)
            {
                KeyValuePair<string, string> header = RequestHeaders.ElementAt(i);
                request.Headers.Add(header.Key, header.Value);
            }

            return request;
        }

        #endregion
    }
}
