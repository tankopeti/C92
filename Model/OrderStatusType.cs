using System.ComponentModel.DataAnnotations;

namespace Cloud9_2.Models
{
    public class OrderStatusType
    {
        [Key]
        public int OrderStatusId { get; set; }
        public string StatusName { get; set; }
        public string Color { get; set; } // New property for color (e.g., hex code)
        public ICollection<Order>? Orders { get; set; }
    }

    // DTO for returning status data
    public class OrderStatusTypeDTO
    {
        public string StatusName { get; set; }
        public string Color { get; set; }
    }
}
