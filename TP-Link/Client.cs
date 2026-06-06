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
        /// Send an SMS to one or several recipients.
        /// </summary>
        /// <param name="sms">SMS object holding the recipients and the message.</param>
        /// <returns>One <see cref="SmsSendResult"/> per recipient.</returns>
        public async Task<List<SmsSendResult>> SendAsync(SmsToSend sms)
        {
            if (sms is null)
                throw new ArgumentNullException(nameof(sms));

            if (sms.Recipients is null || sms.Recipients.Count == 0)
                throw new ArgumentException("At least one recipient is required.", nameof(sms));

            List<SmsSendResult> results = new();
            foreach (string recipient in sms.Recipients)
            {
                Status status = await SendSingleAsync(recipient, sms.Message);
                results.Add(new SmsSendResult { Recipient = recipient, Status = status });
            }

            return results;
        }

        /// <summary>
        /// Send an SMS to one or several recipients (synchronous).
        /// </summary>
        /// <param name="sms">SMS object holding the recipients and the message.</param>
        /// <returns>One <see cref="SmsSendResult"/> per recipient.</returns>
        public List<SmsSendResult> Send(SmsToSend sms)
            => SendAsync(sms).GetAwaiter().GetResult();

        /// <summary>
        /// Send an SMS to a single recipient.
        /// </summary>
        /// <param name="to">Phone number</param>
        /// <param name="message">Message</param>
        /// <returns>Return Status</returns>
        public Status Send(string to, string message)
            => SendSingleAsync(to, message).GetAwaiter().GetResult();

        /// <summary>
        /// Send an SMS to a single recipient (async).
        /// </summary>
        /// <param name="to">Phone number</param>
        /// <param name="message">Message</param>
        /// <returns>Return Status</returns>
        public Task<Status> SendAsync(string to, string message)
            => SendSingleAsync(to, message);

        private async Task<Status> SendSingleAsync(string to, string message)
        {
            Payload payloadSendSms = new()
            {
                Method = TP_ACT.ACT_SET,
                Controller = TP_CONTROLLERS.LTE_SMS_SENDNEWMSG.ToString(),
                Attrs = new Dictionary<string, object>
                {
                    { "index", 1 },
                    { "to", to },
                    { "textContent", message },
                }
            };

            Payload payloadGetSendSmsResult = new()
            {
                Method = TP_ACT.ACT_GET,
                Controller = TP_CONTROLLERS.LTE_SMS_SENDNEWMSG.ToString(),
                Attrs = new Dictionary<string, object>
                {
                    { "sendResult", null }
                }
            };

            ParsedResponse sendResult = await ExecuteRaw(true, payloadSendSms);
            ParsedResponse sentResult = await ExecuteRaw(true, payloadGetSendSmsResult);

            return GetStatus(sendResult, sentResult);
        }

        private Status GetStatus(ParsedResponse send, ParsedResponse sent)
        {
            if (send.Error != 0 || sent.Error != 0)
                return Status.ERROR;

            int? sendResult = _protocol.GetSendResult(sent);
            return sendResult switch
            {
                1 => Status.SENT,
                3 => Status.PROCESSING,
                _ => Status.ERROR
            };
        }

        #endregion

        #region Read

        /// <summary>
        /// Read received SMS (inbox). The router only exposes the last messages it kept.
        /// </summary>
        /// <param name="unreadOnly">null = all messages, true = only unread, false = only read.</param>
        /// <returns>List of <see cref="InboxSms"/>.</returns>
        public async Task<List<InboxSms>> GetInboxAsync(bool? unreadOnly = null)
        {
            Payload resetCursor = new()
            {
                Method = TP_ACT.ACT_SET,
                Controller = TP_CONTROLLERS.LTE_SMS_RECVMSGBOX.ToString(),
                Attrs = new Dictionary<string, object> { { "PageNumber", 1 } }
            };

            Payload listEntries = new()
            {
                Method = TP_ACT.ACT_GL,
                Controller = TP_CONTROLLERS.LTE_SMS_RECVMSGENTRY.ToString(),
                Attrs = AttributeNames("index", "from", "content", "receivedTime", "unread")
            };

            ParsedResponse response = await ExecuteRaw(true, resetCursor, listEntries);
            List<InboxSms> messages = _protocol.MapInbox(response);

            if (unreadOnly.HasValue)
                messages = messages.Where(m => m.Unread == unreadOnly.Value).ToList();

            return messages;
        }

        /// <summary>
        /// Read received SMS (inbox), synchronous.
        /// </summary>
        /// <param name="unreadOnly">null = all messages, true = only unread, false = only read.</param>
        /// <returns>List of <see cref="InboxSms"/>.</returns>
        public List<InboxSms> GetInbox(bool? unreadOnly = null)
            => GetInboxAsync(unreadOnly).GetAwaiter().GetResult();

        /// <summary>
        /// Read only the unread received SMS using the dedicated router endpoint.
        /// </summary>
        /// <returns>List of unread <see cref="InboxSms"/>.</returns>
        public async Task<List<InboxSms>> GetUnreadAsync()
        {
            Payload resetCursor = new()
            {
                Method = TP_ACT.ACT_SET,
                Controller = TP_CONTROLLERS.LTE_SMS_RECVMSGBOX.ToString(),
                Attrs = new Dictionary<string, object> { { "PageNumber", 1 } }
            };

            Payload listEntries = new()
            {
                Method = TP_ACT.ACT_GL,
                Controller = TP_CONTROLLERS.LTE_SMS_UNREADMSGENTRY.ToString(),
                Attrs = AttributeNames("index", "from", "content", "receivedTime", "unread")
            };

            ParsedResponse response = await ExecuteRaw(true, resetCursor, listEntries);
            return _protocol.MapInbox(response);
        }

        /// <summary>
        /// Read only the unread received SMS, synchronous.
        /// </summary>
        /// <returns>List of unread <see cref="InboxSms"/>.</returns>
        public List<InboxSms> GetUnread()
            => GetUnreadAsync().GetAwaiter().GetResult();

        /// <summary>
        /// Read sent SMS (outbox). The router only exposes the last messages it kept.
        /// </summary>
        /// <returns>List of <see cref="OutboxSms"/>.</returns>
        public async Task<List<OutboxSms>> GetOutboxAsync()
        {
            Payload resetCursor = new()
            {
                Method = TP_ACT.ACT_SET,
                Controller = TP_CONTROLLERS.LTE_SMS_SENDMSGBOX.ToString(),
                Attrs = new Dictionary<string, object> { { "PageNumber", 1 } }
            };

            Payload listEntries = new()
            {
                Method = TP_ACT.ACT_GL,
                Controller = TP_CONTROLLERS.LTE_SMS_SENDMSGENTRY.ToString(),
                Attrs = AttributeNames("index", "to", "content", "sendTime")
            };

            ParsedResponse response = await ExecuteRaw(true, resetCursor, listEntries);
            return _protocol.MapOutbox(response);
        }

        /// <summary>
        /// Read sent SMS (outbox), synchronous.
        /// </summary>
        /// <returns>List of <see cref="OutboxSms"/>.</returns>
        public List<OutboxSms> GetOutbox()
            => GetOutboxAsync().GetAwaiter().GetResult();

        private static Dictionary<string, object> AttributeNames(params string[] names)
        {
            Dictionary<string, object> attrs = new();
            foreach (string name in names)
                attrs[name] = null;

            return attrs;
        }

        #endregion

        #region Manage

        /// <summary>
        /// Mark a received SMS as read.
        /// </summary>
        /// <param name="order">
        /// 1-based position (<see cref="InboxSms.Order"/>) returned by <see cref="GetInboxAsync"/>,
        /// not the SMS index. Must be called right after an unfiltered inbox read.
        /// </param>
        /// <returns>True if the router accepted the operation.</returns>
        public async Task<bool> MarkAsReadAsync(int order)
        {
            Payload payload = new()
            {
                Method = TP_ACT.ACT_SET,
                Controller = TP_CONTROLLERS.LTE_SMS_RECVMSGENTRY.ToString(),
                Stack = $"{order},0,0,0,0,0",
                Attrs = new Dictionary<string, object> { { "unread", 0 } }
            };

            ParsedResponse response = await ExecuteRaw(true, payload);
            return response.Error == 0;
        }

        /// <summary>
        /// Delete a received SMS by its 1-based position (<see cref="InboxSms.Order"/>).
        /// Must be called right after an inbox read.
        /// </summary>
        /// <param name="order">1-based position of the SMS in the inbox read.</param>
        /// <returns>True if the router accepted the operation.</returns>
        public Task<bool> DeleteInboxAsync(int order)
            => DeleteAsync(TP_CONTROLLERS.LTE_SMS_RECVMSGENTRY, order);

        /// <summary>
        /// Delete a sent SMS by its 1-based position (<see cref="OutboxSms.Order"/>).
        /// Must be called right after an outbox read.
        /// </summary>
        /// <param name="order">1-based position of the SMS in the outbox read.</param>
        /// <returns>True if the router accepted the operation.</returns>
        public Task<bool> DeleteOutboxAsync(int order)
            => DeleteAsync(TP_CONTROLLERS.LTE_SMS_SENDMSGENTRY, order);

        private async Task<bool> DeleteAsync(TP_CONTROLLERS controller, int order)
        {
            Payload payload = new()
            {
                Method = TP_ACT.ACT_DEL,
                Controller = controller.ToString(),
                Stack = $"{order},0,0,0,0,0",
                Attrs = new Dictionary<string, object>()
            };

            ParsedResponse response = await ExecuteRaw(true, payload);
            return response.Error == 0;
        }

        #endregion

        #region Execute

        /// <summary>
        /// Send a custom payload and get a flattened <see cref="ExtendedPayload"/> back.
        /// Kept for backward compatibility; prefer the typed SMS methods.
        /// </summary>
        /// <param name="payload">Payload to send</param>
        /// <param name="AllowReconnectionOnError">Try reconnection on error</param>
        /// <returns>Return ExtendedPayload</returns>
        public async Task<ExtendedPayload> Execute(Payload payload, bool AllowReconnectionOnError = true)
        {
            ParsedResponse response = await ExecuteRaw(AllowReconnectionOnError, payload);
            ExtendedPayload extended = _protocol.ToExtendedPayload(response);
            extended.Method = payload.Method;
            extended.Controller = payload.Controller;
            return extended;
        }

        /// <summary>
        /// Core execution: encrypt one or several payloads, post them and parse the response.
        /// </summary>
        private async Task<ParsedResponse> ExecuteRaw(bool allowReconnectionOnError, params Payload[] payloads)
        {
            if (!IsReady)
                await Connect();

            string dataFrame = _protocol.MakeDataFrame(payloads);
            string encryptedPayload = EncryptDataFrame(dataFrame);

            try
            {
                ApiRequest request = new ApiRequest($"{_url}/cgi_gdpr", HttpMethod.Post, _cookies);
                request
                    .AddHeader("Accept", "*/*")
                    .AddHeader("Referer", _url)
                    .AddHeader("Cookie", $"loginErrorShow=1; JSESSIONID={_sessionId}")
                    .AddHeader("TokenID", _tokenId)
                    .AddBody(encryptedPayload)
                    ;

                Result<string> result = await request.Run<string>();

                string decryptedPayload = _encryption.AESDecrypt((string)result.Value);
#if DEBUG
                Console.WriteLine(decryptedPayload);
#endif

                return _protocol.ParseResponse(decryptedPayload);
            }
            catch (Exception ex)
            {
                if (ex.Message == HttpStatusCode.InternalServerError.ToString() && allowReconnectionOnError)
                {
                    Reset();
                    return await ExecuteRaw(false, payloads);
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
