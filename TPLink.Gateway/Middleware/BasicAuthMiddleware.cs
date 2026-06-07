using System.Net.Http.Headers;
using System.Text;

namespace TPLink.Gateway.Middleware
{
    /// <summary>
    /// Minimal HTTP Basic authentication, reproducing the reference bridge's
    /// <c>api_users</c> protection. Users are read from the "Authentication:Users"
    /// configuration section (a username/password map). When the map is empty,
    /// authentication is disabled. Swagger and the OpenAPI document are never protected.
    /// </summary>
    public sealed class BasicAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IReadOnlyDictionary<string, string> _users;
        private readonly string _swaggerPrefix;

        public BasicAuthMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _users = configuration.GetSection("Authentication:Users").Get<Dictionary<string, string>>()
                     ?? new Dictionary<string, string>();
            _swaggerPrefix = "/" + (configuration["Swagger:RoutePrefix"] ?? "swagger").Trim('/');
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string path = context.Request.Path.Value ?? string.Empty;

            bool isSwagger = path.StartsWith(_swaggerPrefix, StringComparison.OrdinalIgnoreCase)
                             || path.Equals("/", StringComparison.Ordinal);

            if (_users.Count == 0 || isSwagger)
            {
                await _next(context);
                return;
            }

            if (TryGetCredentials(context, out string user, out string password)
                && _users.TryGetValue(user, out string? expected)
                && string.Equals(expected, password, StringComparison.Ordinal))
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"TPLink.Gateway\", charset=\"UTF-8\"";
        }

        private static bool TryGetCredentials(HttpContext context, out string user, out string password)
        {
            user = string.Empty;
            password = string.Empty;

            string? header = context.Request.Headers.Authorization;
            if (string.IsNullOrEmpty(header))
                return false;

            if (!AuthenticationHeaderValue.TryParse(header, out AuthenticationHeaderValue? parsed)
                || !string.Equals(parsed.Scheme, "Basic", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(parsed.Parameter))
                return false;

            string decoded;
            try
            {
                decoded = Encoding.UTF8.GetString(Convert.FromBase64String(parsed.Parameter));
            }
            catch (FormatException)
            {
                return false;
            }

            int separator = decoded.IndexOf(':');
            if (separator < 0)
                return false;

            user = decoded[..separator];
            password = decoded[(separator + 1)..];
            return true;
        }
    }
}
