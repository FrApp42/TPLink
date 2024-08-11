using FrApp42.TPLink;
using FrApp42.TPLink.Models;

internal class Program
{
    static Client client;

    static void Main(string[] args)
    {
        // See https://aka.ms/new-console-template for more information

        Payload payloadSendSms = new Payload()
        {
            Method = TP_ACT.ACT_SET,
            Controller = TP_CONTROLLERS.LTE_SMS_SENDNEWMSG.ToString(),
            Attrs = new Dictionary<string, object>
            {
                { "index", 1 },
                { "to", "0606060606" },
                { "textContent", $"Hello {DateTime.Now}" },
            }
        };

        Payload payloadGetSendSmsResult = new Payload()
        {
            Method = TP_ACT.ACT_GET,
            Controller = TP_CONTROLLERS.LTE_SMS_SENDNEWMSG.ToString(),
            Attrs = new Dictionary<string, object>
            {
                { "sendResult", null }
            }
        };

        const string url = "http://192.168.1.1";
        const string username = "admin";
        const string password = "admin";

        Task.Run(async () =>
        {
            while (true)
            {
                Console.WriteLine(string.Empty);
                Console.WriteLine("Press Enter to send a new message");
                Console.ReadLine();
                client = new Client(url, username, password);
                try
                {
                    Console.WriteLine("Sending SMS");

                    //Status result = await client.SendAsync("0606060606", $"Hello {DateTime.Now}");
                    Status result = client.Send("0606060606", $"Hello {DateTime.Now}");
                    switch (result)
                    {
                        case Status.SENT:
                            Console.WriteLine("Great! SMS sent successfully");
                            break;
                        case Status.PROCESSING:
                            Console.WriteLine("Warning: SMS sending was accepted but not yet processed.");
                            break;
                        case Status.ERROR:
                        default:
                            Console.WriteLine("Error: SMS could not be sent by router");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                finally
                {
                    //await client.Disconnect();
                }
                
            }                    
        }).GetAwaiter().GetResult();        
    }

    static void VerifySubmission(Payload result)
    {
        if(result.Error == 0)
        {
            Console.WriteLine("Great! SMS send operation was accepted.");
        } else
        {
            throw new Exception("SMS send operation was not accepted");
        }
    }

    static void VerifySubmissionResult(ExtendedPayload result)
    {
        if(result.SendResult != null)
        {
            switch (result.Error)
            {
                case 0 when (int)result.SendResult == 1:
                    Console.WriteLine("Great! SMS sent successfully");
                    break;
                case 0 when (int)result.SendResult == 3:
                    //TODO sendResult=3 means queued or processing ??
                    Console.WriteLine("Warning: SMS sending was accepted but not yet processed.");
                    break;
                default:
                    Console.WriteLine("Error: SMS could not be sent by router");
                    break;
            }
        }        
    }

    static void Dispose()
    {
        if (client.IsReady)
        {
            client.Disconnect();
        }
    }
}

