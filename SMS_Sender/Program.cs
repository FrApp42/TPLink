using FrApp42.TPLink;
using FrApp42.TPLink.Models;

internal class Program
{
    private const string Url = "http://192.168.1.1";
    private const string Username = "admin";
    private const string Password = "admin";

    private static async Task Main(string[] args)
    {
        Client client = new(Url, Username, Password);

        // --- Sending: one SMS object, one or several recipients ---
        SmsToSend sms = new()
        {
            Recipients = { "0606060606", "0707070707" },
            Message = $"Hello from FrApp42.TPLink {DateTime.Now}"
        };

        // For a single recipient: new SmsToSend("0606060606", "Hello");

        Console.WriteLine("Sending SMS...");
        List<SmsSendResult> sendResults = await client.SendAsync(sms);
        foreach (SmsSendResult result in sendResults)
            Console.WriteLine($"  {result.Recipient} -> {result.Status}");

        // --- Reading: received SMS (inbox) ---
        Console.WriteLine();
        Console.WriteLine("Inbox:");
        List<InboxSms> inbox = await client.GetInboxAsync();
        foreach (InboxSms message in inbox)
        {
            string flag = message.Unread ? "[unread]" : "[read]  ";
            Console.WriteLine($"  {flag} #{message.Index} from {message.From} on {message.ReceivedTime:g}: {message.Content}");
        }

        // --- Reading: sent SMS (outbox) ---
        Console.WriteLine();
        Console.WriteLine("Outbox:");
        List<OutboxSms> outbox = await client.GetOutboxAsync();
        foreach (OutboxSms message in outbox)
            Console.WriteLine($"  #{message.Index} to {message.To} on {message.SendTime:g}: {message.Content}");

        await client.Disconnect();
    }
}
