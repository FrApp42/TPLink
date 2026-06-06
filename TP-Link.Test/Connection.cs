using FrApp42.TPLink;

namespace FrApp42.TPLink.Test
{
    [TestClass]
    public class Connection
    {
        const string url = "http://192.168.0.1";
        const string username = "admin";
        const string password = "admin";

        [TestMethod]
        public void AuthTest()
        {
            Client client = new Client(url, username, password);
            try
            {                
                client.Connect().Wait();
            }
            catch (Exception ex)
            {
                
                 Assert.Fail(ex.Message);
                
            }
        }

        [TestMethod]
        public void Send()
        {
            Client client = new Client(url, username, password);
            try
            {
                Status result = client.Send("0606060606", $"Hello {DateTime.Now}");
                if(result == Status.ERROR)
                {
                    Assert.Fail("Error: SMS could not be sent by router");
                }
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void SendToMany()
        {
            Client client = new Client(url, username, password);
            try
            {
                Models.SmsToSend sms = new()
                {
                    Recipients = { "0606060606", "0707070707" },
                    Message = $"Hello {DateTime.Now}"
                };

                List<Models.SmsSendResult> results = client.Send(sms);
                Assert.AreEqual(sms.Recipients.Count, results.Count);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void ReadInbox()
        {
            Client client = new Client(url, username, password);
            try
            {
                List<Models.InboxSms> inbox = client.GetInbox();
                Assert.IsNotNull(inbox);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void ReadOutbox()
        {
            Client client = new Client(url, username, password);
            try
            {
                List<Models.OutboxSms> outbox = client.GetOutbox();
                Assert.IsNotNull(outbox);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }
    }
}