namespace FrApp42.TPLink.Models
{
    /// <summary>
    /// Represents an SMS to send through the router.
    /// Holds one or several recipient phone numbers sharing the same message.
    /// </summary>
    public class SmsToSend
    {
        /// <summary>
        /// One or more recipient phone numbers.
        /// </summary>
        public List<string> Recipients { get; set; } = new();

        /// <summary>
        /// Text content of the message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        public SmsToSend() { }

        /// <summary>
        /// Creates an SMS for a single recipient.
        /// </summary>
        /// <param name="recipient">Recipient phone number.</param>
        /// <param name="message">Message content.</param>
        public SmsToSend(string recipient, string message)
        {
            if (!string.IsNullOrWhiteSpace(recipient))
                Recipients.Add(recipient);

            Message = message;
        }

        /// <summary>
        /// Creates an SMS for several recipients.
        /// </summary>
        /// <param name="recipients">Recipient phone numbers.</param>
        /// <param name="message">Message content.</param>
        public SmsToSend(IEnumerable<string> recipients, string message)
        {
            if (recipients != null)
                Recipients.AddRange(recipients);

            Message = message;
        }
    }
}
