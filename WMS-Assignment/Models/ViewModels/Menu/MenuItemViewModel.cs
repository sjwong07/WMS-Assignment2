using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WMS_Assignment.Models.ViewModels.Menu
{
    public class MenuItemViewModel
    {
        public int MenuItemId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 9999.99, ErrorMessage = "Price must be between 0.01 and 9999.99")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        public string ImageUrl { get; set; }

        public bool IsAvailable { get; set; } = true;

        public bool IsVegetarian { get; set; }

        public bool IsSpicy { get; set; }

        [Range(1, 120, ErrorMessage = "Preparation time must be between 1 and 120 minutes")]
        public int PreparationTime { get; set; } = 15;

        public int DisplayOrder { get; set; }

        public string CategoryName { get; set; }

        public SelectList Categories { get; set; }
    }
}