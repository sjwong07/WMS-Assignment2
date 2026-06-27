using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WMS_Assignment.Models.ViewModels.Order
{
    public class OrderViewModel
    {
        public int OrderId { get; set; }

        public string OrderNumber { get; set; }

        public int UserId { get; set; }

        public string UserName { get; set; }

        [Required(ErrorMessage = "Table selection is required")]
        public int? TableId { get; set; }

        public string TableNumber { get; set; }

        public DateTime OrderDate { get; set; }

        public decimal Subtotal { get; set; }

        public decimal Tax { get; set; }

        public decimal ServiceCharge { get; set; }

        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; }

        public string PaymentStatus { get; set; }

        [StringLength(200)]
        public string SpecialInstructions { get; set; }

        public SelectList Tables { get; set; }

        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();
    }
}