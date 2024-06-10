using TPLink;
using TPLink.Models;

internal class Program
{
    static void Main(string[] args)
    {
        // See https://aka.ms/new-console-template for more information

        
        Payload payloadSendSms = new Payload()
        {
            Method = TP_ACT.ACT_SET,
            Controller = "TP_CONTROLLERS.LTE_SMS_SENDNEWMSG",
            Attrs = new Dictionary<string, object>
            {
                { "index", 1 },
                { "to", "" },
                { "textContent", $"Hello {DateTime.Now}" },
            }
        };

        Payload payloadGetSendSmsResult = new Payload()
        {
            Method = TP_ACT.ACT_GET,
            Controller = "TP_CONTROLLERS.LTE_SMS_SENDNEWMSG",
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
                try
                {
                    Console.WriteLine(string.Empty);
                    Console.WriteLine("Press Enter to send a new message");
                    Console.ReadLine();

                    Console.WriteLine("Sending SMS");
                    Client client = new Client(url, username, password);
                    await client.Connect();

                    Payload result = await client.Execute(payloadSendSms);
                    VerifySubmission(result);

                    Payload sentResult = await client.Execute(payloadGetSendSmsResult);
                    VerifySubmissionResult(sentResult);

                    await client.Disconnect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
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

    static void VerifySubmissionResult(Payload result)
    {
        switch (result.Error)
        {
            case 0 when (int)result.Data[0].SendResult == 1:
                Console.WriteLine("Great! SMS sent successfully");
                break;
            case 0 when (int)result.Data[0].SendResult == 3:
                //TODO sendResult=3 means queued or processing ??
                Console.WriteLine("Warning: SMS sending was accepted but not yet processed.");
                break;
            default:
                Console.WriteLine("Error: SMS could not be sent by router");
                break;
        }
    }
}

