using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS_Assignment.Models.Entities
{
    public class MenuItem
    {
        [Key]
        public int MenuItemId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Price { get; set; }

        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        [StringLength(200)]
        public string ImageUrl { get; set; }

        public bool IsAvailable { get; set; } = true;

        public bool IsVegetarian { get; set; } = false;

        public bool IsSpicy { get; set; } = false;

        public int PreparationTime { get; set; } = 15; // in minutes

        public int DisplayOrder { get; set; } = 0;

        // Navigation properties
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}