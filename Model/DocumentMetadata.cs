namespace Cloud9_2.Models
{
    public class DocumentMetadata
    {
        public int ID { get; set; }
        public int DocumentId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public Document Document { get; set; }
    }
}