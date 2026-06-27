using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS_Assignment.Models.Entities
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public string PaymentMethod { get; set; } // Cash, Credit Card, Debit Card, QR Pay

        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Pending";

        [StringLength(50)]
        public string TransactionId { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [StringLength(200)]
        public string Notes { get; set; }
    }
}