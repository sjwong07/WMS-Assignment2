using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WMS_Assignment.Models;
#nullable disable warnings
public class DB(DbContextOptions options) : DbContext(options)
{

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Table> Tables { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<MenuItem> MenuItems { get; set; }

}


public class Role
{
    [Key, MaxLength(10)]
    public string Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(100)]
    public string Description { get; set; }

    public List<Role> Roles { get; set; } = [];
}
public class User
{
    [Key, MaxLength(100)]
    public string Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name {  get; set; }

    [Required]
    [MaxLength(100)]
    public string Email { get; set; }

    [Required]
    [MaxLength(100)]
    public string Password { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; }


    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string RoleId { get; set; }
    public Role Role { get; set; }

    public List<User> Users { get; set; } = [];

}
public class Category
{
    [Key, MaxLength(10)]
    public string Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(100)]
    public string Description { get; set; }

    public List<Category> Categories { get; set; } = [];

        
}

public class MenuItem
{
    [Key, MaxLength(100)]
    public string Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; }

    [Required]
    [MaxLength(100)]
    public string Description { get; set; }

    [Required]
    public decimal Price { get; set; }

    [MaxLength(50)]
    public string CategoryId { get; set; }
    public Category Category { get; set; }

    public List<MenuItem> MenuItems { get; set; } = [];
}

public class Table
{
    [Key, MaxLength(100)]
    public string Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string Number { get; set; }

    [Required]
    [MaxLength(10)]
    public int Capacity { get; set; }

    [Required]
    [MaxLength(30)]
    public string Location { get; set; }

}

    
