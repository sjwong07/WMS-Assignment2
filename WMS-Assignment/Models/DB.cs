using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WMS_Assignment.Models;

#nullable enable

public class DB(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Table> Tables { get; set; }
    public DbSet<FoodCategory> FoodCategories { get; set; }
    public DbSet<MenuItem> MenuItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Fix decimal precision warnings
        modelBuilder.Entity<MenuItem>()
            .Property(m => m.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderDetail>()
            .Property(od => od.SubTotal)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderDetail>()
            .Property(od => od.UnitPrice)
            .HasPrecision(18, 2);
    }
}

public class Role
{
    [Key, MaxLength(100)]
    public string Id { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Description { get; set; }
}

public class User
{
    [Key, MaxLength(100)]
    public string Id { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Username { get; set; }

    [MaxLength(100)]
    public string? Name { get; set; }

    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }

    

    [MaxLength(200)]
    public string? Hash { get; set; }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [MaxLength(100)]
    public string? RoleId { get; set; }

    public Role? Role { get; set; }

    public int FailedLogin { get; set; } = 0;

    public DateTime? LockoutEnd { get; set; }
}

public class Admin : User
{
}

public class Member : User
{
    [MaxLength(100)]
    public string? PhotoURL { get; set; }
}

public class FoodCategory
{
    [Key, MaxLength(10)]
    public string Id { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

public class MenuItem
{
    [Key, MaxLength(100)]
    public string Id { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Description { get; set; }

    public decimal Price { get; set; }

    [MaxLength(10)]
    public string CategoryId { get; set; } = string.Empty;

    public FoodCategory? Category { get; set; }
}

public class Table
{
    [Key, MaxLength(100)]
    public string Id { get; set; } = string.Empty;

    public int Capacity { get; set; }

    [MaxLength(20)]
    public string? TableType { get; set; }
}

public class Order
{
    [Key, MaxLength(100)]
    public string Id { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? UserId { get; set; }

    public User? User { get; set; }

    [MaxLength(100)]
    public string? TableId { get; set; }

    public Table? Table { get; set; }

    public DateTime OrderDate { get; set; }

    [MaxLength(20)]
    public string? Status { get; set; }

    public decimal TotalAmount { get; set; }

    [MaxLength(20)]
    public string? PaymentMethod { get; set; }

    [MaxLength(20)]
    public string? PaymentStatus { get; set; }

    public List<OrderDetail>? OrderDetails { get; set; }
}

public class OrderDetail
{
    [Key, MaxLength(100)]
    public string Id { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? OrderId { get; set; }

    public Order? Order { get; set; }

    [MaxLength(100)]
    public string? MenuItemId { get; set; }

    public MenuItem? MenuItem { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal SubTotal { get; set; }
}