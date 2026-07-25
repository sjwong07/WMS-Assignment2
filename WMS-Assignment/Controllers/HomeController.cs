using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace WMS_Assignment.Controllers;

public class HomeController(DB db) : Controller
{
    public IActionResult Index()
    {
        return View("~/Views/Security/Login.cshtml");
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
        return View("~/Views/Security/Login.cshtml");
    }

    [HttpPost]
    public async Task<IActionResult> Login(string Username, string Password)
    {
        var user = db.Users.FirstOrDefault(u =>
            u.Username == Username &&
            u.Password == Password);

        if (user != null)
        {
            // Create user identity for authentication cookie
            var claims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Username),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id)
        };

            var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, new System.Security.Claims.ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Home");
        }

        ViewBag.Message = "Invalid Username or Password.";
        return View("~/Views/Security/Register.cshtml"); // or your Login view path
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View("Views/Security/Register.cshtml");
    }

    [HttpPost]
    public IActionResult Register(string Username, string Email, string Password)
    {
        // Check duplicate username
        if (db.Users.Any(u => u.Username == Username))
        {
            ViewBag.Message = "Username already exists.";
            return View("~/Views/Security/Register.cshtml");
        }

        // Generate User ID (U01, U02, ...)
        string newId = "U01";
        var lastUser = db.Users
                         .OrderByDescending(u => u.Id)
                         .FirstOrDefault();

        if (lastUser != null && lastUser.Id.Length > 1 && int.TryParse(lastUser.Id.Substring(1), out int number))
        {
            newId = "U" + (number + 1).ToString("00");
        }

        User user = new User
        {
            Id = newId,
            Username = Username,
            Email = Email,
            Password = Password, // Note: Consider hashing this later for security
            CreatedDate = DateTime.Now,
            RoleId = "RAC01",
            FailedLogin = 0,
            LockoutEnd = null
        };

        try
        {
            db.Users.Add(user);
            db.SaveChanges();

            TempData["Success"] = "Account created successfully! Please login.";
            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            ViewBag.Message = ex.InnerException?.Message ?? ex.Message;
            return View("~/Views/Security/Register.cshtml");
        }
    }


    public IActionResult Cart()
    {
        return View();
    }
}