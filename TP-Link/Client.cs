using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using TPLink.Models;

namespace TPLink
{
    public class Client
    {
        private ApiRequest _apiRequest;
        private Encryption _encryption;
        private Protocol _protocol;

        private string _sessionId { get; set; } = null;
        private string _tokenId { get; set; } = null;

        private string _url { get; set; }
        private string _login { get; set; }
        private string _password { get; set; }

        public Client(string url, string login, string password)
        {
            _url = url;
            _login = login;
            _password = password;

            _apiRequest = new ApiRequest();
            _encryption = new Encryption();
            _protocol = new Protocol();
        }

        public bool IsAuthenticated
        {
            get { return _sessionId != null; }
        }

        public bool IsReady
        {
            get { return _tokenId != null; }
        }

        public async Task Connect()
        {
            EncryptionSettings encryptionSettings = await FetchEncryptionParams();
            ReconfigureEncryption(encryptionSettings);
            await Authenticate();
            await FectchTokenId();
            Console.WriteLine("api_bridge.connect.success");
        }

        private void Reset()
        {
            _sessionId = null;
            _tokenId = null;
        }

        private async Task<EncryptionSettings> FetchEncryptionParams()
        {
            //var parmUrl = _url + "/cgi/getParm";
            //var request = new HttpRequestMessage(HttpMethod.Post, parmUrl);
            //request.Headers.Add("Referer", _url);
            //return await _httpClient.SendAsync(request);

            ApiRequest request = new ApiRequest($"{_url}/cgi/getParm", HttpMethod.Post);
            request.AddHeader("Referer", _url);
            string message = (string)(await request.Run<string>()).Value;

            Regex eeExtractor = new Regex(@"ee=""(\d+)"""); // integer
            Regex nnExtractor = new Regex(@"nn=""([0-9A-F]+)"""); // hex encoded
            Regex seqExtractor = new Regex(@"seq=""(\d+)"""); // integer

            Match eeFound = eeExtractor.Match(message);
            Match nnFound = nnExtractor.Match(message);
            Match seqFound = seqExtractor.Match(message);

            string ee = eeFound.Groups[1].Value; // exponent
            string nn = nnFound.Groups[1].Value; // public key
            string seq = seqFound.Groups[1].Value; // sequence included in authentication signature

            Console.WriteLine($"Received encryption params {ee} {nn} {seq}");

            return new EncryptionSettings { Ee = ee, Nn = nn, Seq = Convert.ToInt32(seq) };
        }

        private void ReconfigureEncryption(EncryptionSettings encryptionSettings)
        {
            _encryption
                .SetSeq(encryptionSettings.Seq.ToString())
                .SetRSAKey(encryptionSettings.Nn, encryptionSettings.Ee)
                .GenAESKey()
                ;
            //_encryption.SetSeq(encryptionSettings.Seq);
            //_encryption.SetRSAKey(encryptionSettings.Nn, encryptionSettings.Ee);
            //_encryption.GenAESKey();

            Console.WriteLine($"Generated AES: {_encryption.GetAESKeyString()}");
            //Console.WriteLine($"Generated AES: {_encryption.AesKeyString}");
        }

        private async Task Authenticate()
        {
            (string data, string sign) = _encryption.AESEncrypt($"{_login}\n{_password}", true);
            Console.WriteLine($"Sending authentication payload {data} {sign.ToLower()}");
            
            //ApiRequest request = new ApiRequest(
            //    $"{_url}/cgi/login?data={HttpUtility.UrlEncode(data)}&sign={sign.ToLower()}&Action=1&LoginStatus=0",
            //    HttpMethod.Post
            //);

            ApiRequest request = new ApiRequest(
                $"{_url}/cgi/login",
                HttpMethod.Post
            );
            request
                .AddHeader("Referer", _url)
                .AddQueryParam("data", data)
                .AddQueryParam("sign", sign.ToLower())
                .AddQueryParam("Action", "1")
                .AddQueryParam("LoginStatus", "0")
                ;

            Result<string> result = await request.Run<string>();

            IEnumerable<string> setCookieHeaderValues;
            if (result.Response.Headers.TryGetValues("set-cookie", out setCookieHeaderValues))
            {
                string setCookieHeader = setCookieHeaderValues.FirstOrDefault();
                Regex sessionIdRegex = new Regex(@"JSESSIONID=([a-f0-9]+)");
                _sessionId = sessionIdRegex.Match(setCookieHeader).Groups[1].Value;               
                if(_sessionId != "de")
                {
                    Console.WriteLine($"Received session cookie {_sessionId}");
                }
                else
                {
                    throw new Exception("Cookie not found");
                }
            } else
            {
                throw new Exception("Cookie not found");
            }
        }

        private async Task FectchTokenId()
        {
            ApiRequest request = new ApiRequest($"{_url}/");
            request
                .AddHeader("Referer", _url)
                .AddHeader("Cookie", $"loginErrorShow=1; JSESSIONID={_sessionId}")
                ;

            Result<string> result = await request.Run<string>();

            var tokenIdRegex = new Regex(@"var token=""([a-f0-9]+)""");
            _tokenId = tokenIdRegex.Match((string)result.Value).Groups[1].Value;
            Console.WriteLine($"Received token id: {_tokenId}");
        }

        private string EncryptDataFrame(string dataFrame)
        {
            Console.WriteLine($"Encrypting: {dataFrame}");
            (string data, string sign) = _encryption.AESEncrypt(dataFrame);
            return $"sign={sign}\r\ndata={data}\r\n";
        }

        public async Task<Payload> Execute(Payload payload, bool AllowReconnectionOnError = true)
        {
            if (!IsReady)
                await Connect();

            string dataFrame = _protocol.MakeDataFrame(payload);
            string encryptedPayload = EncryptDataFrame(dataFrame);

            
            try
            {
                ApiRequest request = new ApiRequest($"{_url}/cgi_gdpr", HttpMethod.Post);
                request
                    .AddHeader("Referer", _url)
                    .AddHeader("Cookie", $"loginErrorShow=1; JSESSIONID={_sessionId}")
                    .AddHeader("TokenID", _tokenId)
                    .AddHeader("Content-Type", "text/plain")
                    .AddBody(encryptedPayload)
                    ;

                Result<string> result = await request.Run<string>();
                string decryptedPayload = _encryption.AESDecrypt((string)result.Value);
                Payload decodedPayload = _protocol.FromDataFrame(decryptedPayload);
                return _protocol.PrettifyResponsePayload(decodedPayload);
            }
            catch (Exception ex)
            {
                if(ex.Message == HttpStatusCode.InternalServerError.ToString() && AllowReconnectionOnError)
                {
                    Reset();
                    return await Execute(payload, false);
                } else
                {
                    throw;
                }                
            }
        }

        public async Task<Payload> Disconnect()
        {
            Payload disconnectPayload = new Payload()
            {
                Method = TP_ACT.ACT_CGI,
                Controller = "/cgi/logout"
            };

            return await Execute(disconnectPayload);
        }
    }
}
