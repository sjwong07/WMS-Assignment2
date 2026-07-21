using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WMS_Assignment.Models;
#nullable disable warnings
public class DB(DbContextOptions options) : DbContext(options)
{

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Table> Tables { get; set; }
    public DbSet<FoodCategory> FoodCategories { get; set; }
    public DbSet<MenuItem> MenuItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }

}


public class Role
{
    [Key, MaxLength(100)]
    public string Id { get; set; }

    [MaxLength(100)]
    public string Description { get; set; }


}
public class User
{
    [Key, MaxLength(100)]
    public string Id { get; set; }


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
public class FoodCategory
{
    [Key, MaxLength(10)]
    public string Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; }

}

public class MenuItem
{
    [Key, MaxLength(100)]
    public string Id { get; set; }

    [MaxLength(50)]
    public string Name { get; set; }


    [MaxLength(100)]
    public string Description { get; set; }


    public decimal Price { get; set; }

    [MaxLength(10)]
    public string CategoryId { get; set; }
    public FoodCategory Category { get; set; }


}

public class Table
{
    [Key, MaxLength(100)]
    public string Id { get; set; }


    [MaxLength(10)]
    public int Capacity { get; set; }

    [MaxLength(20)]
    public string TableType { get; set; }

}
public class Order
{
    [Key, MaxLength(100)]
    public string Id { get; set; }

    [MaxLength(100)]
    public string UserId { get; set; }
    public User User { get; set; }

    [MaxLength(100)]
    public string TableId { get; set; }
    public Table Table { get; set; }

    public DateTime OrderDate { get; set; }

    // e.g. Pending, Preparing, Served, Completed, Cancelled
    [MaxLength(20)]
    public string Status { get; set; }

    public decimal TotalAmount { get; set; }

    // e.g. Cash, Card, E-Wallet
    [MaxLength(20)]
    public string PaymentMethod { get; set; }

    // e.g. Unpaid, Paid
    [MaxLength(20)]
    public string PaymentStatus { get; set; }

    public List<OrderDetail> OrderDetails { get; set; }
}

public class OrderDetail
{
    [Key, MaxLength(100)]
    public string Id { get; set; }

    [MaxLength(100)]
    public string OrderId { get; set; }
    public Order Order { get; set; }

    [MaxLength(100)]
    public string MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; }

    public int Quantity { get; set; }

    // price snapshot at time of order, in case MenuItem price changes later
    public decimal UnitPrice { get; set; }

    public decimal SubTotal { get; set; }
}