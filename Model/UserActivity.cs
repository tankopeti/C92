using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class UserActivity
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }
        public bool IsActive { get; set; }
    }
}