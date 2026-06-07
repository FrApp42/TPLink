namespace TPLink.Gateway.Configuration
{
    /// <summary>
    /// Swagger / OpenAPI settings, bound from the "Swagger" configuration section.
    /// </summary>
    public sealed class SwaggerOptions
    {
        public const string SectionName = "Swagger";

        /// <summary>Enable or disable the Swagger JSON document and the Swagger UI.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>API title shown in the Swagger UI.</summary>
        public string Title { get; set; } = "Archer MR600 bridge API";

        /// <summary>API version used as the Swagger document name.</summary>
        public string Version { get; set; } = "v1";

        /// <summary>API description shown in the Swagger UI.</summary>
        public string Description { get; set; } = "Open Source API bridge for Archer MR600";

        /// <summary>Route prefix where the Swagger UI is served (empty = served at the root).</summary>
        public string RoutePrefix { get; set; } = "swagger";
    }
}
