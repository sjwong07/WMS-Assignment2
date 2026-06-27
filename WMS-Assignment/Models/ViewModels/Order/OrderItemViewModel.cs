using System.ComponentModel.DataAnnotations;

namespace WMS_Assignment.Models.ViewModels.Order
{
    public class OrderItemViewModel
    {
        public int MenuItemId { get; set; }

        [Required(ErrorMessage = "Menu item is required")]
        public string MenuItemName { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal Subtotal { get; set; }

        [StringLength(200)]
        public string SpecialInstructions { get; set; }
    }
}