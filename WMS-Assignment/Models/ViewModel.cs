using System.ComponentModel.DataAnnotations;

namespace WMS_Assignment.Models;
#nullable disable warnings

// Dont Touch this file first




public class RoleVM
{
    
    public string? Id { get; set; }

    [MaxLength(100)]
    public string Description { get; set; }


}
public class UserVM
{
    
    public string? Id { get; set; }

    [MaxLength(100)]
    public string Username { get; set; }

    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(100)]
    public string Email { get; set; }

    [MaxLength(100)]
    public string Password { get; set; }

    [MaxLength(100)]
    public string FirstName { get; set; }

    [MaxLength(100)]
    public string LastName { get; set; }

    public DateTime CreatedDate { get; set; }

    [MaxLength(100)]
    public string RoleId { get; set; }

    public Role Role { get; set; }
}
public class FoodCategoryVM
{
   
    public string? Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; }

}

public class MenuItemVM
{
    public string? Search {  get; set; }
    public List<string> SelectCategories { get; set; }
    public decimal? MinPrice {  get; set; }
    public decimal? MaxPrice {  get; set; }

    public IEnumerable<FoodCategory> FoodCategories { get; set; }
    public IEnumerable<MenuItem> MenuItems {  get; set; }


}

public class TableVM
{
   
    public string? Id { get; set; }


    [MaxLength(10)]
    public int Capacity { get; set; }

    [MaxLength(20)]
    public string TableType { get; set; }

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

public class MenuVM
{
    public List<FoodCategoryVM> FoodCategories { get; set; }
    public List<MenuItemVM> MenuItems { get; set; }
}