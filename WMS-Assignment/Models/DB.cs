using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WMS_Assignment.Models;
#nullable disable warnings
public class DB(DbContextOptions options) : DbContext(options)
{

    public DbSet<User> Users => Set<User>();
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

    [Required(ErrorMessage = "Username is required")]
    [MaxLength(100)]
    public string Username { get; set; }

    [MaxLength(100)]
    public string Name { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
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

    public int FailedLogin { get; set; } = 0;

    public DateTime? LockoutEnd { get; set; }
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

public class AccountController : Controller
{
    private readonly DB _db;

    public AccountController(DB db)
    {
        _db = db;
    }

    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // POST: /Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(User model)
    {
        // 1. Check if model state is valid based on Data Annotations
        if (ModelState.IsValid)
        {
            // 2. Generate a primary key since User.Id is a string
            model.Id = Guid.NewGuid().ToString();

            // 3. Set mandatory fields that aren't captured in the small form
            model.CreatedDate = DateTime.Now;
            model.FailedLogin = 0;

            // Assign a default RoleId if required by your application (ensure this role exists in your Roles table)
            // model.RoleId = "DEFAULT_ROLE_ID"; 

            // 4. Add to database context and save
            _db.Users.Add(model);
            await _db.SaveChangesAsync();

            // 5. Redirect to login after successful registration
            return RedirectToAction("Login");
        }

        // If validation fails, return the form with errors
        ViewBag.Error = "Please fix the errors below.";
        return View(model);
    }
}