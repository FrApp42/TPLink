namespace FrApp42.TPLink.Models
{
    /// <summary>
    /// Low-level representation of a decoded router response frame:
    /// an error code and a list of raw attribute objects.
    /// </summary>
    internal class ParsedResponse
    {
        public int Error { get; set; }
        public List<Dictionary<string, string>> Data { get; set; } = new();
    }
}
