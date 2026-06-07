using Microsoft.AspNetCore.Mvc;

namespace TPLink.Gateway.Controllers
{
    /// <summary>
    /// Monitoring endpoints, reproducing the reference bridge's monitoring routes.
    /// </summary>
    [ApiController]
    [Route("api/v1/monitoring")]
    [Tags("Monitoring")]
    public sealed class MonitoringController : ControllerBase
    {
        /// <summary>
        /// Prometheus metrics endpoint (work in progress, as in the reference bridge).
        /// </summary>
        [HttpGet("metrics")]
        [Produces("text/plain")]
        public IActionResult Metrics() => Content("WIP", "text/plain");
    }
}
