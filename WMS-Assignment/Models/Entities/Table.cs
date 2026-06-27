using System.ComponentModel.DataAnnotations;

namespace WMS_Assignment.Models.Entities
{
    public class Table
    {
        [Key]
        public int TableId { get; set; }

        [Required]
        [StringLength(10)]
        public string TableNumber { get; set; }

        [Range(1, 20)]
        public int Capacity { get; set; }

        [StringLength(50)]
        public string Location { get; set; }

        public bool IsOccupied { get; set; } = false;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}