using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS_Assignment.Models.Entities
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        [StringLength(20)]
        public string OrderNumber { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public int? TableId { get; set; }

        [ForeignKey("TableId")]
        public virtual Table Table { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(10, 2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal Tax { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal ServiceCharge { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(20)]
        public string OrderStatus { get; set; } = "Pending"; // Pending, Confirmed, Preparing, Ready, Served, Completed, Cancelled

        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Refunded

        [StringLength(200)]
        public string SpecialInstructions { get; set; }

        public DateTime? EstimatedReadyTime { get; set; }

        public DateTime? CompletedDate { get; set; }

        // Navigation properties
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual Payment Payment { get; set; }
    }
}