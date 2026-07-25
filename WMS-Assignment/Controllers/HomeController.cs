using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WMS_Assignment.Controllers;

public class HomeController(DB db) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }



    public IActionResult Receipt()
    {
        return View();
    }


    [HttpGet]
    public IActionResult Login()
    {
        return View("Views/Security/Login.cshtml");
    }

    [HttpPost]
    public IActionResult Login(string Username, string Password)
    {
        var user = db.Users.FirstOrDefault(u =>
    u.Username == Username &&
    u.Password == Password);

        if (user != null)
        {
            ViewBag.Message = "Login Successful!";
        }
        else
        {
            ViewBag.Message = "Invalid Username or Password.";
        }

        return View();
    }


    [HttpGet]
    public IActionResult Register()
    {
        return View("Views/Security/Register.cshtml");
    }

    [HttpPost]
    public IActionResult Register(string FirstName, string LastName,
                              string Name, string Email, string Username,
                              string Password, string ConfirmPassword)
    {
        if (Password != ConfirmPassword)
        {
            ViewBag.Message = "Passwords do not match.";
            return View("Views/Security/Register.cshtml");
        }

        User user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = Username,
            Name = Name,
            Email = Email,
            Password = Password,
            FirstName = FirstName,
            LastName = LastName,
            CreatedDate = DateTime.Now,
            RoleId = "RAC01"
        };

        db.Users.Add(user);

        ViewBag.Message = "RoleId saved: " + user.RoleId;

        db.SaveChanges();

        ViewBag.Message = "Account created successfully!";
        return View("Views/Security/Register.cshtml");
    }


    public IActionResult Cart()
    {
        return View();
    }
}