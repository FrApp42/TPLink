namespace FrApp42.TPLink.Models
{
    /// <summary>
    /// Represents a received SMS (inbox entry) as stored on the router.
    /// </summary>
    public class InboxSms
    {
        /// <summary>
        /// SMS id as stored in the router.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Phone number of the sender.
        /// </summary>
        public string? From { get; set; }

        /// <summary>
        /// Text content of the message.
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Date and time the message was received.
        /// </summary>
        public DateTime ReceivedTime { get; set; }

        /// <summary>
        /// Whether the message is still marked as unread.
        /// </summary>
        public bool Unread { get; set; }

        /// <summary>
        /// Position of the SMS (1-based) inside the last messages returned by the router.
        /// This value, not <see cref="Index"/>, is required to mark the SMS as read or to delete it.
        /// </summary>
        public int Order { get; set; }
    }
}
