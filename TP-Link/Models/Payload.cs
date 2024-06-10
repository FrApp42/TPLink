namespace TPLink.Models
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
        public int Error { get; set; }
        public List<DataObject> Data { get; set; }
    }

    public class DataObject
    {
        public int Index { get; set; }
        public int SendResult { get; set; }
        public bool Unread { get; set; }
        public DateTime ReceivedTime { get; set; }
        public DateTime SendTime { get; set; }
        public string Content { get; set; }
    }
}
