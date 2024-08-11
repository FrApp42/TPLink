using System.Net;
using System.Text.RegularExpressions;
using FrApp42.TPLink.Models;

namespace FrApp42.TPLink
{
    public class Client
    {
        #region Private Variables

        private CookieContainer _cookies = new();
        private ApiRequest _apiRequest;
        private Encryption _encryption;
        private Protocol _protocol;

        private string _sessionId { get; set; } = null;
        private string _tokenId { get; set; } = null;

        private string _url { get; set; }
        private string _login { get; set; }
        private string _password { get; set; }

        #endregion

        #region Public Variables

        public bool IsAuthenticated
        {
            get { return _sessionId != null; }
        }

        public bool IsReady
        {
            get { return _tokenId != null; }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url">Routeur url</param>
        /// <param name="login">Routeur username (usally admin)</param>
        /// <param name="password">Routeur password</param>
        public Client(string url, string login, string password)
        {
            _url = url;
            _login = login;
            _password = password;

            _apiRequest = new ApiRequest(_cookies);
            _encryption = new Encryption();
            _protocol = new Protocol();
        }

        #endregion

        #region Connection

        /// <summary>
        /// Initiate Routeur connection
        /// </summary>
        /// <returns></returns>
        public async Task Connect()
        {
            EncryptionSettings encryptionSettings = await FetchEncryptionParams();
            ReconfigureEncryption(encryptionSettings);
            await Authenticate();
            await FectchTokenId();
#if DEBUG
            Console.WriteLine("api_bridge.connect.success");
            Console.WriteLine();
#endif
        }

        /// <summary>
        /// Send Disconnect payload
        /// </summary>
        /// <returns>Disconnect Payload</returns>
        public async Task<Payload> Disconnect()
        {
            Payload disconnectPayload = new Payload()
            {
                Method = TP_ACT.ACT_CGI,
                Controller = "/cgi/logout"
            };

            return await Execute(disconnectPayload);
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

#if DEBUG
            Console.WriteLine($"Received encryption params");
            Console.WriteLine($"RSA n : {nn}");
            Console.WriteLine($"RSA e : {ee}");
            Console.WriteLine($"Sequence : {seq}");
#endif

            return new EncryptionSettings { Ee = ee, Nn = nn, Seq = Convert.ToInt32(seq) };
        }

        private void ReconfigureEncryption(EncryptionSettings encryptionSettings)
        {
            _encryption
                .SetSeq(encryptionSettings.Seq.ToString())
                .SetRSAKey(encryptionSettings.Nn, encryptionSettings.Ee)
                .GenAESKey()
                ;
#if DEBUG
            Console.WriteLine($"Generated AES: {_encryption.GetAESKeyString()}");
#endif
        }

        private async Task Authenticate()
        {
            (string data, string sign) = _encryption.AESEncrypt(_login + "\n" + _password, true);
#if DEBUG
            Console.WriteLine($"Sending authentication payload {data} {sign.ToLower()}");
#endif


            ApiRequest request = new ApiRequest(
                $"{_url}/cgi/login",
                HttpMethod.Post,
                _cookies
            );
            request
                .AddHeader("Referer", _url)
                .AddQueryParam("data", data)
                .AddQueryParam("sign", sign)
                .AddQueryParam("Action", "1")
                .AddQueryParam("LoginStatus", "0")
                ;

            Result<string> result = await request.Run<string>();

#if DEBUG
            Console.WriteLine(await result.Response.Content.ReadAsStringAsync());
#endif

            IEnumerable<string> setCookieHeaderValues;
            if (result.Response.Headers.TryGetValues("set-cookie", out setCookieHeaderValues))
            {
                string setCookieHeader = setCookieHeaderValues.FirstOrDefault();
                Regex sessionIdRegex = new Regex(@"JSESSIONID=([a-f0-9]+)");
                _sessionId = sessionIdRegex.Match(setCookieHeader).Groups[1].Value;
                if (_sessionId != "de")
                {
#if DEBUG
                    Console.WriteLine($"Received session cookie {_sessionId}");
                    Console.WriteLine();
#endif
                }
                else
                {
                    throw new Exception("Cookie not found");
                }
            }
            else
            {
                throw new Exception("Cookie not found");
            }
        }

        #endregion

        #region Token

        private async Task FectchTokenId()
        {
            ApiRequest request = new ApiRequest($"{_url}/", _cookies);
            request
                .AddHeader("Referer", _url)
                .AddHeader("Cookie", $"loginErrorShow=1; JSESSIONID={_sessionId}")
                ;

            Result<string> result = await request.Run<string>();

            var tokenIdRegex = new Regex(@"var token=""([a-f0-9]+)""");
            _tokenId = tokenIdRegex.Match((string)result.Value).Groups[1].Value;

#if DEBUG
            Console.WriteLine($"Received token id: {_tokenId}");
            Console.WriteLine();
#endif
        }

        #endregion

        #region Send       

        /// <summary>
        /// Send SMS
        /// </summary>
        /// <param name="to">Phone number</param>
        /// <param name="message">Message</param>
        /// <returns>Return Status</returns>
        public Status Send(string to, string message)
        {
            Payload payloadSendSms = new Payload()
            {
                Method = TP_ACT.ACT_SET,
                Controller = TP_CONTROLLERS.LTE_SMS_SENDNEWMSG.ToString(),
                Attrs = new Dictionary<string, object>
            {
                { "index", 1 },
                { "to", $"{to}" },
                { "textContent", $"{message}" },
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

            ExtendedPayload sendResult = Execute(payloadSendSms).GetAwaiter().GetResult();
            ExtendedPayload sentResult = Execute(payloadGetSendSmsResult).GetAwaiter().GetResult();
            return GetStatus(sendResult, sentResult);
        }

        /// <summary>
        /// Send SMS Async
        /// </summary>
        /// <param name="to">Phone number</param>
        /// <param name="message">Message</param>
        /// <returns>Return Status</returns>
        public async Task<Status> SendAsync(string to, string message)
        {

            Payload payloadSendSms = new Payload()
            {
                Method = TP_ACT.ACT_SET,
                Controller = TP_CONTROLLERS.LTE_SMS_SENDNEWMSG.ToString(),
                Attrs = new Dictionary<string, object>
            {
                { "index", 1 },
                { "to", $"{to}" },
                { "textContent", $"{message}" },
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

            ExtendedPayload sendResult = await Execute(payloadSendSms);
            ExtendedPayload sentResult = await Execute(payloadGetSendSmsResult);

            return GetStatus(sendResult, sentResult);
        }

        private Status GetStatus(ExtendedPayload send, ExtendedPayload sent)
        {
            if (send.Error == 0 && sent.Error == 0)
            {
                if (sent.SendResult == null)
                {
                    return Status.ERROR;
                }

                switch (sent.SendResult)
                {
                    case 1:
                        return Status.SENT;
                    case 3:
                        return Status.PROCESSING;
                    default:
                        throw new Exception("Uncategorized status");
                }
            }

            return Status.ERROR;
        }

        /// <summary>
        /// Send custom Payload
        /// </summary>
        /// <param name="payload">Payload to send</param>
        /// <param name="AllowReconnectionOnError">Try reconnection on error</param>
        /// <returns>Return ExtendedPayload</returns>
        public async Task<ExtendedPayload> Execute(Payload payload, bool AllowReconnectionOnError = true)
        {
            if (!IsReady)
                await Connect();

            string dataFrame = _protocol.MakeDataFrame(payload);
            string encryptedPayload = EncryptDataFrame(dataFrame);

            try
            {
                ApiRequest request = new ApiRequest($"{_url}/cgi_gdpr", HttpMethod.Post, _cookies);
                request
                    .AddHeader("Accept", "*/*")
                    .AddHeader("Referer", _url)
                    .AddHeader("Cookie", $"loginErrorShow=1; JSESSIONID={_sessionId}")
                    .AddHeader("TokenID", _tokenId)
                    //.AddHeader("Content-Type", "text/plain")
                    .AddBody(encryptedPayload)
                    ;

                Result<string> result = await request.Run<string>();

                string decryptedPayload = _encryption.AESDecrypt((string)result.Value);
#if DEBUG
                Console.WriteLine(decryptedPayload);
#endif

                //Payload decodedPayload = _protocol.FromDataFrame(decryptedPayload);

                //return _protocol.PrettifyResponsePayload(decodedPayload);
                return _protocol.ExtendedFromDataFrame(decryptedPayload);
            }
            catch (Exception ex)
            {
                if (ex.Message == HttpStatusCode.InternalServerError.ToString() && AllowReconnectionOnError)
                {
                    Reset();
                    return await Execute(payload, false);
                }
                else
                {
                    throw;
                }
            }
        }

        private string EncryptDataFrame(string dataFrame)
        {
#if DEBUG
            Console.WriteLine($"Encrypting: {dataFrame}");
#endif
            (string data, string sign) = _encryption.AESEncrypt(dataFrame);
            return $"sign={sign}\r\ndata={data}\r\n";
        }

        #endregion

    }
}
