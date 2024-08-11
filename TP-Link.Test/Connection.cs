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
    }
}