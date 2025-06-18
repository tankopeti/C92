using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class ChatMessage
    {
    public int Id { get; set; }
    public string Group { get; set; }
    public string SenderUserName { get; set; }
    public string Message { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}