using System.Text.RegularExpressions;
using FrApp42.TPLink.Models;

namespace FrApp42.TPLink
{
    internal class Protocol
    {
        public string MakeDataFrame(Payload payload)
        {
            string attrs = ToKv(payload.Attrs);

            int nbrAtts = attrs != null && Regex.Matches(attrs, @"\r\n").Count > 0 ? Regex.Matches(attrs, @"\r\n").Count : 0;

            var header = (int)payload.Method;
            var data = "[" + payload.Controller + "#" + (payload.Stack ?? "0,0,0,0,0,0") + "#" + "0,0,0,0,0,0" + "]" + "0," + nbrAtts + "\r\n" + attrs;

            return header + "\r\n" + data;
        }

        public Payload FromDataFrame(string dataFrame)
        {
            string[] lines = dataFrame.Trim().Split("\n");
            int error = 0;

            Payload payload = new Payload();

            Regex objectHeaderExtractor = new Regex(@"\[\d,\d,\d,\d,\d,\d\]\d"); // ex [0,0,0,0,0,0]0
            Regex objectAttributeExtractor = new Regex(@"^([a-zA-Z0-9]+)=(.*)$"); // ex totalNumber=11
            Regex frameErrorExtractor = new Regex(@"^\[error\](\d+)$"); // ex [error]0

            DataObject currentObject = null;
            List<DataObject> data = new List<DataObject>();
            foreach (string line in lines)
            {
                Match matching = objectHeaderExtractor.Match(line);
                // found header
                if (matching.Success)
                {
                    if (currentObject != null)
                    {
                        data.Add(currentObject);
                    }
                    currentObject = new DataObject();
                    continue;
                }

                matching = frameErrorExtractor.Match(line);
                // found error code
                if (matching.Success)
                {
                    error = int.Parse(matching.Groups[1].Value);
                    if (currentObject != null)
                    {
                        data.Add(currentObject);
                    }
                    continue;
                }

                // found attribute
                matching = objectAttributeExtractor.Match(line);
                if (matching.Success)
                {
                    string propertyName = matching.Groups[1].Value;
                    string propertyValue = matching.Groups[2].Value;
                    switch (propertyName)
                    {
                        case "sendResult":
                            currentObject.SendResult = Convert.ToInt32(propertyValue);
                            break;
                        case "sendTime":
                            break;
                        case "unread":
                            break;
                        case "receivedTime":
                            break;
                        case "from":
                            currentObject.From = propertyValue;
                            break;
                        case "content":
                            break;
                    }
                    continue;
                }
            }

            return new Payload
            {
                Error = error,
                Data = data,
            };
        }

        public ExtendedPayload ExtendedFromDataFrame(string dataFrame)
        {
            string stack = FindHeaderString(dataFrame, new Regex(@"\[(.*?)\]"), 1);

            ExtendedPayload payload = new ExtendedPayload()
            {
                Error = FindHeader<int>(dataFrame, new Regex(@"\[error\](\d+)"), 1),
                Stack = !string.IsNullOrEmpty(stack) ? stack : null,
                SendResult = FindValue<int>(dataFrame, "sendResult")
            };
            return payload;
        }

        private T? FindHeader<T>(string text, Regex regex, int group) where T : struct
        {
            Match match = regex.Match(text);
            if (match.Success)
            {
                string value = match.Groups[group].Value;
                return typeof(T) switch
                {
                    Type t when t == typeof(string) => (T)(object)value.ToString(),
                    Type t when t == typeof(int) => (T)(object)int.Parse(value),
                    Type t when t == typeof(DateTime) => (T)(object)DateTime.Parse(value),
                    // Ajoutez d'autres types de conversion si nécessaire
                    _ => throw new InvalidOperationException("Type non supporté")
                };
            }
            return null;
        }

        private string FindHeaderString(string text, Regex regex, int group)
        {
            Match match = regex.Match(text);
            if (match.Success)
            {
                return match.Groups[group].Value;
            }
            return string.Empty;
        }

        private T? FindValue<T>(string text, string fieldName, string pattern = null) where T : struct
        {
            if (string.IsNullOrEmpty(pattern))
            {
                pattern = $@"{fieldName}=(.+)";
            }
            Regex regex = new Regex(pattern);
            Match match = regex.Match(text);
            if (match.Success)
            {
                string value = match.Groups[1].Value;
                return typeof(T) switch
                {
                    Type t when t == typeof(string) => (T)(object)value.ToString(),
                    Type t when t == typeof(int) => (T)(object)int.Parse(value),
                    Type t when t == typeof(DateTime) => (T)(object)DateTime.Parse(value),
                    // Ajoutez d'autres types de conversion si nécessaire
                    _ => throw new InvalidOperationException("Type non supporté")
                };
            }

            return null;
        }

        public Payload PrettifyResponsePayload(Payload payload)
        {
            payload.Error = int.Parse(payload.Error.ToString());
            foreach (DataObject dataObject in payload.Data)
            {
                dataObject.Index = int.Parse(dataObject.Index.ToString());
                dataObject.SendResult = int.Parse(dataObject.SendResult.ToString());
                dataObject.Unread = int.Parse(dataObject.Unread.ToString()) > 0;
                dataObject.ReceivedTime = DateTime.Parse(dataObject.ReceivedTime.ToString());
                dataObject.SendTime = DateTime.Parse(dataObject.SendTime.ToString());
                dataObject.Content = dataObject.Content.Replace("\u0012", "\n");
            }
            return payload;
        }

        private string ObjectToKv(object obj, string keyValueSeparator, string lineSeparator)
        {
            string ret = "";
            foreach (var prop in obj.GetType().GetProperties())
            {
                if (prop.GetValue(obj, null) != null || prop.GetValue(obj, null).ToString() == "0" || prop.GetValue(obj, null).ToString() == "")
                {
                    var value = prop.PropertyType == typeof(string) ? Regex.Replace(prop.GetValue(obj, null).ToString(), @"(\r\n|\n|\r)", "\u0012") : prop.GetValue(obj, null);
                    ret += prop.Name + keyValueSeparator + value + lineSeparator;
                }
                else
                {
                    ret += prop.Name + lineSeparator;
                }
            }
            return ret;
        }

        private string ToKv(object data, string keyValueSeparator = "=", string lineSeparator = "\r\n")
        {
            if (data == null)
            {
                return "";
            }

            if (data.GetType() == typeof(string))
            {
                return data.ToString();
            }

            if (data.GetType() == typeof(List<object>))
            {
                return string.Join(lineSeparator, (List<object>)data) + lineSeparator;
            }
            if (data.GetType() == typeof(Dictionary<string, object>))
            {
                string ret = "";
                foreach (KeyValuePair<string, object> kvp in (Dictionary<string, object>)data)
                {
                    string value = string.Empty;
                    if (kvp.Value != null)
                    {
                        value = (kvp.Value.GetType() == typeof(string)) ? kvp.Value.ToString().Replace("\r\n", "\u0012").Replace("\n", "\u0012").Replace("\r", "\u0012") : kvp.Value.ToString();
                        ret += kvp.Key + keyValueSeparator + value + lineSeparator;
                    }
                    else
                    {
                        ret += kvp.Key + lineSeparator;
                    }
                }
                return ret;
            }

            return ObjectToKv(data, keyValueSeparator, lineSeparator);
        }
    }

}
