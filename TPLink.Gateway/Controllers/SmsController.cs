using FrApp42.TPLink;
using FrApp42.TPLink.Models;
using Microsoft.AspNetCore.Mvc;
using TPLink.Gateway.Models;

namespace TPLink.Gateway.Controllers
{
    /// <summary>
    /// SMS management endpoints, reproducing the routes of the
    /// <c>plewin/tp-link-modem-router</c> bridge on top of the FrApp42.TP-Link library.
    /// </summary>
    [ApiController]
    [Route("api/v1/sms")]
    [Produces("application/json")]
    [Tags("SMS")]
    public sealed class SmsController : ControllerBase
    {
        private readonly Client _client;

        public SmsController(Client client) => _client = client;

        /// <summary>
        /// Get received SMS (the last messages the router kept, most recent first).
        /// </summary>
        /// <param name="unread">
        /// Optional filter: <c>true</c> returns only unread SMS, <c>false</c> only read SMS,
        /// omitted returns all. Due to protocol limitations, <c>unread=false</c> results are
        /// inaccurate while <c>unread=true</c> reports correctly.
        /// </param>
        [HttpGet("inbox")]
        public async Task<IActionResult> GetInbox([FromQuery] bool? unread)
        {
            List<InboxSms> messages = await _client.GetInboxAsync(unread);
            return Ok(new { status = 200, data = messages });
        }

        /// <summary>
        /// Mark the n-th received SMS as read.
        /// </summary>
        /// <remarks>
        /// Stateful at the router's end: call this only right after an unfiltered
        /// <c>GET /api/v1/sms/inbox</c>. <paramref name="smsOrderNumber"/> is the 1-based
        /// position in that read, not the SMS index.
        /// </remarks>
        [HttpPatch("inbox/{smsOrderNumber:int:min(1)}")]
        public async Task<IActionResult> MarkInboxAsRead(int smsOrderNumber)
        {
            bool ok = await _client.MarkAsReadAsync(smsOrderNumber);
            return Ok(new { status = 200, data = ok });
        }

        /// <summary>
        /// Delete the n-th received SMS.
        /// </summary>
        /// <remarks>
        /// Stateful: call this only right after an unfiltered <c>GET /api/v1/sms/inbox</c>.
        /// <paramref name="smsOrderNumber"/> is the 1-based position in that read.
        /// </remarks>
        [HttpDelete("inbox/{smsOrderNumber:int:min(1)}")]
        public async Task<IActionResult> DeleteInbox(int smsOrderNumber)
        {
            bool ok = await _client.DeleteInboxAsync(smsOrderNumber);
            return Ok(new { status = 200, data = ok });
        }

        /// <summary>
        /// Get sent SMS (the last messages the router kept).
        /// </summary>
        [HttpGet("outbox")]
        public async Task<IActionResult> GetOutbox()
        {
            List<OutboxSms> messages = await _client.GetOutboxAsync();
            return Ok(new { status = 200, data = messages });
        }

        /// <summary>
        /// Send a new SMS. Accepts a JSON or form-urlencoded body with <c>to</c> and <c>content</c>.
        /// </summary>
        [HttpPost("outbox")]
        [Consumes("application/json", "application/x-www-form-urlencoded")]
        public async Task<IActionResult> SendSms([FromBody] NewSmsRequest? request = null)
        {
            string? to = request?.To;
            string? content = request?.Content;

            if ((to is null || content is null) && Request.HasFormContentType)
            {
                IFormCollection form = await Request.ReadFormAsync();
                to ??= form["to"].ToString();
                content ??= form["content"].ToString();
            }

            if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(content))
                return StatusCode(StatusCodes.Status400BadRequest, new { status = 400 });

            List<SmsSendResult> results = await _client.SendAsync(new SmsToSend(to, content));
            return Ok(new { status = 200, data = results });
        }

        /// <summary>
        /// Delete the n-th sent SMS.
        /// </summary>
        /// <remarks>
        /// Stateful: call this only right after a <c>GET /api/v1/sms/outbox</c>.
        /// <paramref name="smsOrderNumber"/> is the 1-based position in that read.
        /// </remarks>
        [HttpDelete("outbox/{smsOrderNumber:int:min(1)}")]
        public async Task<IActionResult> DeleteOutbox(int smsOrderNumber)
        {
            bool ok = await _client.DeleteOutboxAsync(smsOrderNumber);
            return Ok(new { status = 200, data = ok });
        }
    }
}
