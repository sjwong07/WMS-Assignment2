using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using WMS_Assignment.Models.Entities;

namespace WMS_Assignment.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Table> Tables { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Admin", Description = "System Administrator" },
                new Role { RoleId = 2, RoleName = "Staff", Description = "Restaurant Staff" },
                new Role { RoleId = 3, RoleName = "Customer", Description = "Customer" }
            );

            // Seed Admin User
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    Email = "admin@restaurant.com",
                    PasswordHash = "admin123", // In production, use BCrypt or similar
                    FirstName = "Admin",
                    LastName = "User",
                    PhoneNumber = "0123456789",
                    RoleId = 1,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                }
            );

            // Seed Sample Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, CategoryName = "Appetizers", Description = "Starters and small plates", DisplayOrder = 1, IsActive = true },
                new Category { CategoryId = 2, CategoryName = "Main Course", Description = "Main dishes", DisplayOrder = 2, IsActive = true },
                new Category { CategoryId = 3, CategoryName = "Desserts", Description = "Sweet treats", DisplayOrder = 3, IsActive = true },
                new Category { CategoryId = 4, CategoryName = "Beverages", Description = "Drinks and refreshments", DisplayOrder = 4, IsActive = true }
            );

            // Seed Sample Menu Items
            modelBuilder.Entity<MenuItem>().HasData(
                new MenuItem { MenuItemId = 1, Name = "Spring Rolls", Description = "Crispy vegetable spring rolls with sweet chili sauce", Price = 12.90m, CategoryId = 1, IsAvailable = true, IsVegetarian = true, PreparationTime = 10 },
                new MenuItem { MenuItemId = 2, Name = "Chicken Satay", Description = "Grilled chicken skewers with peanut sauce", Price = 15.90m, CategoryId = 1, IsAvailable = true, IsVegetarian = false, IsSpicy = true, PreparationTime = 15 },
                new MenuItem { MenuItemId = 3, Name = "Nasi Lemak", Description = "Fragrant rice with sambal, fried chicken, and sides", Price = 18.90m, CategoryId = 2, IsAvailable = true, IsVegetarian = false, IsSpicy = true, PreparationTime = 20 },
                new MenuItem { MenuItemId = 4, Name = "Chocolate Lava Cake", Description = "Warm chocolate cake with molten center", Price = 14.90m, CategoryId = 3, IsAvailable = true, IsVegetarian = true, PreparationTime = 15 },
                new MenuItem { MenuItemId = 5, Name = "Mango Smoothie", Description = "Fresh mango blended with yogurt", Price = 9.90m, CategoryId = 4, IsAvailable = true, IsVegetarian = true, PreparationTime = 5 }
            );

            // Seed Sample Tables
            modelBuilder.Entity<Table>().HasData(
                new Table { TableId = 1, TableNumber = "T01", Capacity = 2, Location = "Window", IsOccupied = false },
                new Table { TableId = 2, TableNumber = "T02", Capacity = 4, Location = "Center", IsOccupied = false },
                new Table { TableId = 3, TableNumber = "T03", Capacity = 6, Location = "VIP Room", IsOccupied = false },
                new Table { TableId = 4, TableNumber = "T04", Capacity = 2, Location = "Patio", IsOccupied = false }
            );
        }
    }
}