namespace FrApp42.TPLink.Models
{
    public class Payload
    {
        public TP_ACT Method { get; set; }
        public string Controller { get; set; }
        public string Stack { get; set; } = "0,0,0,0,0,0";
        public string PStack { get; set; } = "0,0,0,0,0,0";
        public Dictionary<string, object> Attrs { get; set; }
        public int NbAttrs
        {
            get
            {
                return Attrs.Count;
            }
        }
        public int? Error { get; set; } = 0;
        public List<DataObject> Data { get; set; } = new List<DataObject>();
    }

    public class ExtendedPayload : Payload
    {
        public int? PageNumber { get; set; }
        public int? totalNumber { get; set; }

        public int? Index { get; set; }
        public string? To { get; set; }
        public string? From { get; set; }
        public string? Content { get; set; }
        public string? SendTime { get; set; }
        public int? SendResult { get; set; }
    }

    public class DataObject
    {
        public int Index { get; set; }
        public int SendResult { get; set; }
        public string From { get; set; }
        public bool Unread { get; set; }
        public DateTime ReceivedTime { get; set; }
        public DateTime SendTime { get; set; }
        public string Content { get; set; }
    }
}
