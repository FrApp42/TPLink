namespace FrApp42.TPLink.Models
{
    /// <summary>
    /// Represents a sent SMS (outbox entry) as stored on the router.
    /// </summary>
    public class OutboxSms
    {
        /// <summary>
        /// SMS id as stored in the router.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Phone number of the recipient.
        /// </summary>
        public string? To { get; set; }

        /// <summary>
        /// Text content of the message.
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Date and time the message was sent.
        /// </summary>
        public DateTime SendTime { get; set; }

        /// <summary>
        /// Position of the SMS (1-based) inside the last messages returned by the router.
        /// This value, not <see cref="Index"/>, is required to delete the SMS.
        /// </summary>
        public int Order { get; set; }
    }
}
