using System.ComponentModel.DataAnnotations;

namespace WMS_Assignment.Models;

#nullable disable warnings




public class RoleVM
{
    
    public string? Id { get; set; }

    [MaxLength(100)]
    public string Description { get; set; }



}

public class LoginVM
{
    [MaxLength(100)]
    public string Username { get; set; }

    [MaxLength(20)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    public bool RememberMe {  get; set; }
}






public class RegisterVM
{
    [Required, MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [StringLength(16, MinimumLength = 12, ErrorMessage = "Password must be between 12 and 16 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IFormFile? ProfilePhoto { get; set; }
}
public class MenuItemVM
    {
        public string? Search { get; set; }
        public List<string> SelectCategories { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public IEnumerable<FoodCategory> FoodCategories { get; set; }
        public IEnumerable<MenuItem> MenuItems { get; set; }


    }

public class ProfileVM
{
    public string UserId {  get; set; }
    public string Username {  get; set; }
    public string Password { get; set; }
    public string Email { get; set;}
    public string ProfilePhoto {  get; set; }


}

    public class OrderVM
    {

        public string? Id { get; set; }

        [MaxLength(100)]
        public string UserId { get; set; }
        public User User { get; set; }

        [MaxLength(100)]
        public string TableId { get; set; }
        public Table Table { get; set; }

        public DateTime OrderDate { get; set; }


        [MaxLength(20)]
        public string Status { get; set; }

        public decimal TotalAmount { get; set; }


        [MaxLength(20)]
        public string PaymentMethod { get; set; }

        // e.g. Unpaid, Paid
        [MaxLength(20)]
        public string PaymentStatus { get; set; }

        public List<OrderDetail> OrderDetails { get; set; }
    }

    public class ProductInsertVM
{
    [StringLength(5)]
    [RegularExpression(@"P\d{3}", ErrorMessage = "Invalid {0} format.")]
    public string Id { get; set; }

    [StringLength(100)]
   
    public string Name { get; set; }

    [StringLength(200)]
    public string  description{ get; set; }


    [Range(0.01,999.99)]
    [RegularExpression(@"\d+(\.\d{1,2})?", ErrorMessage = "Invalid {0} format.")]
    public string Price { get; set; }
    [Required]
    public string CategoryId { get; set; }

    public List<IFormFile>? Photos { get; set; }

    public IEnumerable<FoodCategory> FoodCategories { get; set; }

}


    public class ProductUpdateVM
{
    public string Id {  get; set; }

    [StringLength(5)]
    [RegularExpression(@"P\d{3}", ErrorMessage = "Invalid {0} format.")]
    public string Name { get; set; }

    [StringLength(200)]
    public string description { get; set; }


    [Range(0.01, 999.99)]
    [RegularExpression(@"\d+(\.\d{1,2})?", ErrorMessage = "Invalid {0} format.")]
    public string Price { get; set; }

    [Required]
    public string CategoryId { get; set; }

    public IFormFile[] Photos { get; set; }

    public string? CurrentPhotoURL { get; set; }

    public IEnumerable<FoodCategory> FoodCategories { get; set; }

}

public class OrderDetailVM
    {

        public string? Id { get; set; }

        [MaxLength(100)]
        public string OrderId { get; set; }
        public Order Order { get; set; }

        [MaxLength(100)]
        public string MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal SubTotal { get; set; }
    }

    public class forgotPasswordVM
    {
        [MaxLength(50)]
        [EmailAddress]
        public string Email { get; set; }

    }
