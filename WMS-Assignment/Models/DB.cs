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
    public int Id { get; set; }


    [MaxLength(10)]
    public int Capacity { get; set; }

    [MaxLength(20)]
    public string TableType { get; set; }

}