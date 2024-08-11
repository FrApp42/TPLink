namespace FrApp42.TPLink
{
    internal interface IApiRequest
    {
        public string URL { get; }
        public HttpMethod Method { get; }
        public Dictionary<string, string> RequestHeaders { get; }
        public Dictionary<string, string> ContentHeaders { get; }
        public Dictionary<string, string> QueryParams { get; }
        public object Body { get; }

        public ApiRequest AddHeader(string key, string value);
        public ApiRequest AddHeaders(Dictionary<string, string> headers);
        public ApiRequest AddContentHeader(string key, string value);
        public ApiRequest AddQueryParam(string key, string value);
        public ApiRequest AddQueryParams(Dictionary<string, string> queryParams);
        public ApiRequest AddBody(string body);

        public Task<Result<T>> Run<T>();
    }
}
