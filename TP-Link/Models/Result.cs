namespace FrApp42.TPLink
{
    internal class Result<T>
    {
        public int StatusCode { get; set; }
        public object Value { get; set; }

        public HttpResponseMessage Response { get; set; }
    }
}
