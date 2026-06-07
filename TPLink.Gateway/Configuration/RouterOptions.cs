namespace TPLink.Gateway.Configuration
{
    /// <summary>
    /// Connection settings for the TP-Link router, bound from the "Router" configuration section.
    /// </summary>
    public sealed class RouterOptions
    {
        public const string SectionName = "Router";

        /// <summary>Router base URL, e.g. <c>http://192.168.1.1</c>.</summary>
        public string Url { get; set; } = "http://192.168.1.1";

        /// <summary>Router username (usually <c>admin</c>).</summary>
        public string Login { get; set; } = "admin";

        /// <summary>Router password.</summary>
        public string Password { get; set; } = string.Empty;
    }
}
