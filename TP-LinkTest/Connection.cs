using TPLink;

namespace TP_LinkTest
{
    [TestClass]
    public class Connection
    {
        const string url = "http://192.168.1.1";
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
    }
}