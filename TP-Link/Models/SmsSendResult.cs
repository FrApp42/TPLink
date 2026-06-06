namespace FrApp42.TPLink.Models
{
    /// <summary>
    /// Result of sending an SMS to a single recipient.
    /// </summary>
    public class SmsSendResult
    {
        /// <summary>
        /// Recipient phone number.
        /// </summary>
        public string Recipient { get; set; } = string.Empty;

        /// <summary>
        /// Sending status reported by the router.
        /// </summary>
        public Status Status { get; set; }

        /// <summary>
        /// True when the message was sent or accepted for processing by the router.
        /// </summary>
        public bool IsSuccess => Status == Status.SENT || Status == Status.PROCESSING;
    }
}
