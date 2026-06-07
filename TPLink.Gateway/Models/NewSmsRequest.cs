namespace TPLink.Gateway.Models
{
    /// <summary>
    /// Payload accepted by <c>POST /api/v1/sms/outbox</c> to send a new SMS.
    /// Mirrors the reference bridge's <c>OutboxNewSms</c> schema.
    /// </summary>
    public sealed class NewSmsRequest
    {
        /// <summary>Phone number of the receiver, e.g. <c>+33123456789</c>.</summary>
        public string? To { get; set; }

        /// <summary>SMS content.</summary>
        public string? Content { get; set; }
    }
}
