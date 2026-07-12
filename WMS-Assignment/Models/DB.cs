using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WMS_Assignment.Models;
#nullable disable warnings

public class DB(DbContextOptions options) : DbContext(options)
{
    // ===== AUTHENTICATION TABLES =====
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<LoginAttempt> LoginAttempts { get; set; }

    // ===== PRODUCT & MENU TABLES =====
    public DbSet<FoodCategory> FoodCategories { get; set; }
    public DbSet<MenuItem> MenuItems { get; set; }
    public DbSet<MenuItemPhoto> MenuItemPhotos { get; set; }

    // ===== ORDERING TABLES =====
    public DbSet<Table> Tables { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Payment> Payments { get; set; }

    // ===== CONFIGURATION =====
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure all decimal properties (Precision: 18, Scale: 2)
        var decimalProperties = new[]
        {
            // MenuItem
            modelBuilder.Entity<MenuItem>().Property(m => m.Price),
            // Order
            modelBuilder.Entity<Order>().Property(o => o.Subtotal),
            modelBuilder.Entity<Order>().Property(o => o.Tax),
            modelBuilder.Entity<Order>().Property(o => o.ServiceCharge),
            modelBuilder.Entity<Order>().Property(o => o.TotalAmount),
            // OrderItem
            modelBuilder.Entity<OrderItem>().Property(oi => oi.UnitPrice),
            // Payment
            modelBuilder.Entity<Payment>().Property(p => p.Amount),
        };

        foreach (var property in decimalProperties)
        {
            property.HasPrecision(18, 2);
        }

        // Configure relationships
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Table)
            .WithMany(t => t.Orders)
            .HasForeignKey(o => o.TableId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.MenuItem)
            .WithMany(m => m.OrderItems)
            .HasForeignKey(oi => oi.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Order)
            .WithOne(o => o.Payment)
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MenuItemPhoto>()
            .HasOne(mp => mp.MenuItem)
            .WithMany(m => m.Photos)
            .HasForeignKey(mp => mp.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LoginAttempt>()
            .HasOne(la => la.User)
            .WithMany(u => u.LoginAttempts)
            .HasForeignKey(la => la.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ============================================================
// ===== 1. AUTHENTICATION MODELS =====
// ============================================================

public class Role
{
    [Key, MaxLength(100)]
    public string Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(100)]
    public string Description { get; set; }

    // Navigation
    public ICollection<User> Users { get; set; }
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

    // Password Reset Fields
    [MaxLength(100)]
    public string PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiry { get; set; }

    // Account Lockout Fields
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEndDate { get; set; }
    public bool IsLockedOut { get; set; } = false;

    // Navigation
    public Role Role { get; set; }
    public ICollection<Order> Orders { get; set; }
    public ICollection<LoginAttempt> LoginAttempts { get; set; }
}

public class LoginAttempt
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string UserId { get; set; }

    public DateTime AttemptDate { get; set; }

    [MaxLength(50)]
    public string IpAddress { get; set; }

    public bool IsSuccessful { get; set; }

    [MaxLength(255)]
    public string FailureReason { get; set; }

    // Navigation
    public User User { get; set; }
}

// ============================================================
// ===== 2. PRODUCT & MENU MODELS =====
// ============================================================

public class FoodCategory
{
    [Key, MaxLength(10)]
    public string Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(255)]
    public string Description { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; } = 0;

    // Navigation
    public ICollection<MenuItem> MenuItems { get; set; }
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

    public bool IsAvailable { get; set; } = true;

    public bool IsVegetarian { get; set; } = false;

    public bool IsSpicy { get; set; } = false;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime? UpdatedDate { get; set; }

    [MaxLength(255)]
    public string DefaultImageUrl { get; set; }

    // Navigation
    public FoodCategory Category { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; }
    public ICollection<MenuItemPhoto> Photos { get; set; }
}

public class MenuItemPhoto
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string MenuItemId { get; set; }

    [Required]
    [MaxLength(500)]
    public string ImageUrl { get; set; }

    [MaxLength(255)]
    public string Caption { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool IsPrimary { get; set; } = false;

    public DateTime UploadedDate { get; set; } = DateTime.Now;

    // Navigation
    public MenuItem MenuItem { get; set; }
}

// ============================================================
// ===== 3. ORDERING & PAYMENT MODELS =====
// ============================================================

public class Table
{
    [Key, MaxLength(100)]
    public int Id { get; set; }

    [MaxLength(10)]
    public int Capacity { get; set; }

    [MaxLength(10)]
    public string Number { get; set; }

    [MaxLength(30)]
    public string Location { get; set; }

    public bool IsOccupied { get; set; } = false;

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Order> Orders { get; set; }
}

public class Order
{
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public DateTime OrderDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    [Required]
    public decimal Subtotal { get; set; }

    [Required]
    public decimal Tax { get; set; }

    [Required]
    public decimal ServiceCharge { get; set; }

    [Required]
    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    public string SpecialInstructions { get; set; }

    // Foreign Keys
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; }

    [Required]
    public int TableId { get; set; }

    // Navigation
    public User User { get; set; }
    public Table Table { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public Payment Payment { get; set; }
}

public class OrderItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public decimal UnitPrice { get; set; }

    [MaxLength(255)]
    public string SpecialInstructions { get; set; }

    // Foreign Keys
    [Required]
    [MaxLength(100)]
    public string OrderId { get; set; }

    [Required]
    [MaxLength(100)]
    public string MenuItemId { get; set; }

    // Navigation
    public Order Order { get; set; }
    public MenuItem MenuItem { get; set; }
}

public class Payment
{
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public DateTime PaymentDate { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(20)]
    public string PaymentMethod { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Completed";

    [MaxLength(100)]
    public string TransactionId { get; set; }

    [MaxLength(500)]
    public string ReceiptNumber { get; set; }

    // Foreign Key
    [Required]
    [MaxLength(100)]
    public string OrderId { get; set; }

    // Navigation
    public Order Order { get; set; }
}