namespace Cloud9_2.Models
{
    public class DocumentLink
    {
        public int ID { get; set; }
        public int DocumentId { get; set; }
        public string ModuleID { get; set; }
        public int RecordID { get; set; }
        public Document Document { get; set; }
    }
}