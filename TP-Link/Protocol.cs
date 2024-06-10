using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TPLink.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TPLink
{
    
    internal class Protocol
    {
        //public string MakeDataFrame(List<object> payload)
        //{
        //    if (payload == null)
        //    {
        //        payload = new List<object>();
        //    }

        //    var sections = payload.Select(payloadItem =>
        //    {
        //        var attrs = payloadItem.GetType().GetProperty("attrs").GetValue(payloadItem, null);

        //        var stack = payloadItem.GetType().GetProperty("stack") != null ? payloadItem.GetType().GetProperty("stack").GetValue(payloadItem, null).ToString() : "0,0,0,0,0,0";
        //        var pStack = "0,0,0,0,0,0"; // not used
        //        attrs = ToKv(attrs);

        //        return new
        //        {
        //            method = payloadItem.GetType().GetProperty("method").GetValue(payloadItem, null),
        //            controller = payloadItem.GetType().GetProperty("controller").GetValue(payloadItem, null),
        //            stack = stack,
        //            pStack = pStack,
        //            attrs = attrs,
        //            nbAttrs = attrs != null ? attrs.ToString().Split(new string[] { "\r\n" }, StringSplitOptions.None).Length : 0
        //        };
        //    }).ToList();

        //    int index = 0;

        //    var header = string.Join("&", sections.Select(section => section.method));
        //    var data = string.Join("", sections.Select(s => "[" + s.controller + "#" + s.stack + "#" + s.pStack + "]" + (index++) + "," + s.nbAttrs + "\r\n" + s.attrs));

        //    return header + "\r\n" + data;
        //}

        //public string MakeDataFrame(List<Payload> payload)
        //{
        //    if (payload == null)
        //        payload = new List<Payload>();

        //    var sections = payload.Select(payloadItem =>
        //    {
        //        var attrs = ToKv(payloadItem.Attrs);

        //        return new
        //        {
        //            method = payloadItem.Method,
        //            controller = payloadItem.Controller,
        //            stack = payloadItem.Stack ?? "0,0,0,0,0,0",
        //            pStack = "0,0,0,0,0,0", // not used
        //            attrs = attrs,
        //            nbAttrs = attrs != null ? attrs.Split(new string[] { "\r\n" }, StringSplitOptions.None).Length : 0
        //        };
        //    }).ToList();

        //    int index = 0;

        //    var header = string.Join("&", sections.Select(section => section.method));
        //    var data = string.Join("", sections.Select(s => "[" + s.controller + "#" + s.stack + "#" + s.pStack + "]" + (index++) + "," + s.nbAttrs + "\r\n" + s.attrs));

        //    return header + "\r\n" + data;
        //}

        public string MakeDataFrame(Payload payload)
        {
            string attrs = ToKv(payload.Attrs);

            var section = new
            {
                method = payload.Method,
                controller = payload.Controller,
                stack = payload.Stack ?? "0,0,0,0,0,0",
                pStack = "0,0,0,0,0,0", // not used
                attrs = attrs,
                nbAttrs = attrs != null ? attrs.Split(new string[] { "\r\n" }, StringSplitOptions.None).Length : 0
            };

            var header = section.method;
            var data = "[" + section.controller + "#" + section.stack + "#" + section.pStack + "]" + "0," + section.nbAttrs + "\r\n" + section.attrs;

            return header + "\r\n" + data;
        }

        //public object FromDataFrame(string dataFrame)
        //{
        //    var lines = dataFrame.Trim().Split("\n");
        //    int error = 0;

        //    var objectHeaderExtractor = new Regex(@"\[\d,\d,\d,\d,\d,\d\]\d"); // ex [0,0,0,0,0,0]0
        //    var objectAttributeExtractor = new Regex(@"^([a-zA-Z0-9]+)=(.*)$"); // ex totalNumber=11
        //    var frameErrorExtractor = new Regex(@"^\error\$"); // ex [error]0

        //    Dictionary<string, string> currentObject = null;
        //    var data = new List<Dictionary<string, string>>();
        //    foreach (var line in lines)
        //    {
        //        var matching = objectHeaderExtractor.Match(line);
        //        // found header
        //        if (matching.Success)
        //        {
        //            if (currentObject != null)
        //            {
        //                data.Add(currentObject);
        //            }
        //            currentObject = new Dictionary<string, string>();
        //            continue;
        //        }

        //        matching = frameErrorExtractor.Match(line);
        //        // found error code
        //        if (matching.Success)
        //        {
        //            error = int.Parse(matching.Groups[1].Value);
        //            if (currentObject != null)
        //            {
        //                data.Add(currentObject);
        //            }
        //            continue;
        //        }

        //        // found attribute
        //        matching = objectAttributeExtractor.Match(line);
        //        if (matching.Success)
        //        {
        //            currentObject[matching.Groups[1].Value] = matching.Groups[2].Value;
        //            continue;
        //        }
        //    }

        //    return new
        //    {
        //        error,
        //        data,
        //    };
        //}

        public Payload FromDataFrame(string dataFrame)
        {
            string[] lines = dataFrame.Trim().Split("\n");
            int error = 0;

            Regex objectHeaderExtractor = new Regex(@"\[\d,\d,\d,\d,\d,\d\]\d"); // ex [0,0,0,0,0,0]0
            Regex objectAttributeExtractor = new Regex(@"^([a-zA-Z0-9]+)=(.*)$"); // ex totalNumber=11
            Regex frameErrorExtractor = new Regex(@"^\error\$"); // ex [error]0

            DataObject currentObject = null;
            List<DataObject> data = new List<DataObject>();
            foreach (var line in lines)
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
                    PropertyInfo property = currentObject.GetType().GetProperty(propertyName);
                    if (property != null)
                    {
                        switch (Type.GetTypeCode(property.PropertyType))
                        {
                            case TypeCode.Int32:
                                property.SetValue(currentObject, int.Parse(propertyValue));
                                break;
                            case TypeCode.Boolean:
                                property.SetValue(currentObject, int.Parse(propertyValue) > 0);
                                break;
                            case TypeCode.DateTime:
                                property.SetValue(currentObject, DateTime.Parse(propertyValue));
                                break;
                            case TypeCode.String:
                                property.SetValue(currentObject, propertyValue.Replace("\u0012", "\n"));
                                break;
                        }                        
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

        //public object PrettifyResponsePayload(object payload)
        //{
        //    payload.GetType().GetProperty("error").SetValue(payload, int.Parse(payload.GetType().GetProperty("error").GetValue(payload, null).ToString()));

        //    var integerTypedAttributes = new List<string>
        //    {
        //        "index",
        //        "sendResult",
        //    };

        //    var booleanTypedAttributes = new List<string>
        //    {
        //        "unread",
        //    };

        //    var dateTypedAttributes = new List<string>
        //    {
        //        "receivedTime",
        //        "sendTime",
        //    };

        //    var stringTypedAttributes = new List<string>
        //    {
        //        "content",
        //    };

        //    var data = (List<object>)payload.GetType().GetProperty("data").GetValue(payload, null);
        //    for (int i = 0; i < data.Count; i++)
        //    {
        //        var payloadObject = data[i];
        //        foreach (var key in payloadObject.GetType().GetProperties().Select(p => p.Name))
        //        {
        //            if (integerTypedAttributes.Contains(key))
        //            {
        //                payloadObject.GetType().GetProperty(key).SetValue(payloadObject, int.Parse(payloadObject.GetType().GetProperty(key).GetValue(payloadObject, null).ToString()));
        //            }
        //            else if (booleanTypedAttributes.Contains(key))
        //            {
        //                payloadObject.GetType().GetProperty(key).SetValue(payloadObject, int.Parse(payloadObject.GetType().GetProperty(key).GetValue(payloadObject, null).ToString()) > 0);
        //            }
        //            else if (dateTypedAttributes.Contains(key))
        //            {
        //                payloadObject.GetType().GetProperty(key).SetValue(payloadObject, DateTime.Parse(payloadObject.GetType().GetProperty(key).GetValue(payloadObject, null).ToString()));
        //            }
        //            else if (stringTypedAttributes.Contains(key))
        //            {
        //                payloadObject.GetType().GetProperty(key).SetValue(payloadObject, payloadObject.GetType().GetProperty(key).GetValue(payloadObject, null).ToString().Replace("\u0012", "\n"));
        //            }
        //        }
        //    }

        //    return payload;
        //}

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

        public string ObjectToKv(object obj, string keyValueSeparator, string lineSeparator)
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

        public string ToKv(object data, string keyValueSeparator = "=", string lineSeparator = "\r\n")
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

            return ObjectToKv(data, keyValueSeparator, lineSeparator);
        }
    }

}
