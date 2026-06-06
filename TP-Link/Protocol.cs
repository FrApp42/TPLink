using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FrApp42.TPLink.Models;

namespace FrApp42.TPLink
{
    internal class Protocol
    {
        private static readonly Regex ObjectHeaderExtractor = new(@"^\[\d+,\d+,\d+,\d+,\d+,\d+\]\d+"); // ex [0,0,0,0,0,0]0
        private static readonly Regex ObjectAttributeExtractor = new(@"^([a-zA-Z0-9_]+)=(.*)$");      // ex totalNumber=11
        private static readonly Regex FrameErrorExtractor = new(@"^\[error\](-?\d+)$");                // ex [error]0

        #region Encode

        /// <summary>
        /// Builds a data frame for one or several payloads.
        /// Multi-payload frames are required to combine a cursor reset with a list read,
        /// the same way the original bridge does.
        /// </summary>
        public string MakeDataFrame(params Payload[] payloads)
        {
            if (payloads == null || payloads.Length == 0)
                throw new ArgumentException("At least one payload is required", nameof(payloads));

            string header = string.Join("&", payloads.Select(p => ((int)p.Method).ToString()));

            StringBuilder data = new();
            for (int index = 0; index < payloads.Length; index++)
            {
                Payload payload = payloads[index];
                string attrs = ToKv(payload.Attrs);
                int nbAttrs = string.IsNullOrEmpty(attrs) ? 0 : Regex.Matches(attrs, @"\r\n").Count;
                string stack = string.IsNullOrEmpty(payload.Stack) ? "0,0,0,0,0,0" : payload.Stack;

                data.Append('[').Append(payload.Controller).Append('#').Append(stack)
                    .Append('#').Append("0,0,0,0,0,0").Append(']')
                    .Append(index).Append(',').Append(nbAttrs).Append("\r\n")
                    .Append(attrs);
            }

            return header + "\r\n" + data.ToString();
        }

        #endregion

        #region Decode

        /// <summary>
        /// Parses a decrypted data frame into an error code and a list of raw attribute objects.
        /// </summary>
        public ParsedResponse ParseResponse(string dataFrame)
        {
            ParsedResponse response = new();
            if (string.IsNullOrEmpty(dataFrame))
                return response;

            string[] lines = dataFrame.Replace("\r\n", "\n").Trim().Split('\n');
            Dictionary<string, string>? current = null;

            foreach (string line in lines)
            {
                // found object header
                if (ObjectHeaderExtractor.IsMatch(line))
                {
                    if (current != null)
                        response.Data.Add(current);

                    current = new Dictionary<string, string>();
                    continue;
                }

                // found error code (terminates the last object)
                Match errorMatch = FrameErrorExtractor.Match(line);
                if (errorMatch.Success)
                {
                    response.Error = int.Parse(errorMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    if (current != null)
                    {
                        response.Data.Add(current);
                        current = null;
                    }
                    continue;
                }

                // found attribute
                Match attrMatch = ObjectAttributeExtractor.Match(line);
                if (attrMatch.Success && current != null)
                    current[attrMatch.Groups[1].Value] = attrMatch.Groups[2].Value;
            }

            if (current != null)
                response.Data.Add(current);

            return response;
        }

        /// <summary>
        /// Maps a parsed response to a list of received SMS (inbox).
        /// </summary>
        public List<InboxSms> MapInbox(ParsedResponse response)
        {
            List<InboxSms> messages = new();
            int order = 1;

            foreach (Dictionary<string, string> entry in response.Data)
            {
                if (!entry.ContainsKey("index"))
                    continue;

                messages.Add(new InboxSms
                {
                    Index = GetInt(entry, "index"),
                    From = GetString(entry, "from"),
                    Content = Unescape(GetString(entry, "content")),
                    ReceivedTime = GetDate(entry, "receivedTime"),
                    Unread = GetInt(entry, "unread") > 0,
                    Order = order++
                });
            }

            return messages;
        }

        /// <summary>
        /// Maps a parsed response to a list of sent SMS (outbox).
        /// </summary>
        public List<OutboxSms> MapOutbox(ParsedResponse response)
        {
            List<OutboxSms> messages = new();
            int order = 1;

            foreach (Dictionary<string, string> entry in response.Data)
            {
                if (!entry.ContainsKey("index"))
                    continue;

                messages.Add(new OutboxSms
                {
                    Index = GetInt(entry, "index"),
                    To = GetString(entry, "to"),
                    Content = Unescape(GetString(entry, "content")),
                    SendTime = GetDate(entry, "sendTime"),
                    Order = order++
                });
            }

            return messages;
        }

        /// <summary>
        /// Extracts the sendResult value from a parsed response, if present.
        /// </summary>
        public int? GetSendResult(ParsedResponse response)
        {
            foreach (Dictionary<string, string> entry in response.Data)
            {
                if (entry.TryGetValue("sendResult", out string? value) &&
                    int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                {
                    return parsed;
                }
            }

            return null;
        }

        /// <summary>
        /// Flattens a parsed response into an <see cref="ExtendedPayload"/> (backward-compatibility helper).
        /// </summary>
        public ExtendedPayload ToExtendedPayload(ParsedResponse response)
        {
            ExtendedPayload payload = new()
            {
                Error = response.Error,
                SendResult = GetSendResult(response)
            };

            Dictionary<string, string>? first = response.Data.FirstOrDefault(d => d.Count > 0);
            if (first != null)
            {
                if (first.ContainsKey("index"))
                    payload.Index = GetInt(first, "index");

                payload.To = GetString(first, "to");
                payload.From = GetString(first, "from");
                payload.Content = Unescape(GetString(first, "content"));

                if (first.TryGetValue("sendTime", out string? sendTime))
                    payload.SendTime = sendTime;
            }

            return payload;
        }

        #endregion

        #region Decode helpers

        private static string? GetString(Dictionary<string, string> entry, string key)
            => entry.TryGetValue(key, out string? value) ? value : null;

        private static int GetInt(Dictionary<string, string> entry, string key)
            => entry.TryGetValue(key, out string? value) &&
               int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : 0;

        private static DateTime GetDate(Dictionary<string, string> entry, string key)
        {
            if (!entry.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
                return default;

            if (DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime exact))
                return exact;

            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed)
                ? parsed
                : default;
        }

        private static string? Unescape(string? content)
            => content?.Replace("\u0012", "\n");

        #endregion

        #region Key/Value encoding

        private string ToKv(object? data, string keyValueSeparator = "=", string lineSeparator = "\r\n")
        {
            if (data == null)
                return string.Empty;

            if (data is string text)
                return text;

            if (data is Dictionary<string, object> dict)
            {
                StringBuilder ret = new();
                foreach (KeyValuePair<string, object> kvp in dict)
                {
                    if (kvp.Value != null)
                    {
                        string value = kvp.Value is string str
                            ? str.Replace("\r\n", "\u0012").Replace("\n", "\u0012").Replace("\r", "\u0012")
                            : kvp.Value.ToString() ?? string.Empty;

                        ret.Append(kvp.Key).Append(keyValueSeparator).Append(value).Append(lineSeparator);
                    }
                    else
                    {
                        // attribute name only (used by list reads, ex: ACT_GL)
                        ret.Append(kvp.Key).Append(lineSeparator);
                    }
                }
                return ret.ToString();
            }

            return ObjectToKv(data, keyValueSeparator, lineSeparator);
        }

        private string ObjectToKv(object obj, string keyValueSeparator, string lineSeparator)
        {
            StringBuilder ret = new();
            foreach (var prop in obj.GetType().GetProperties())
            {
                object? value = prop.GetValue(obj, null);
                if (value != null)
                {
                    string formatted = value is string str
                        ? Regex.Replace(str, @"(\r\n|\n|\r)", "\u0012")
                        : value.ToString() ?? string.Empty;

                    ret.Append(prop.Name).Append(keyValueSeparator).Append(formatted).Append(lineSeparator);
                }
                else
                {
                    ret.Append(prop.Name).Append(lineSeparator);
                }
            }
            return ret.ToString();
        }

        #endregion
    }
}
