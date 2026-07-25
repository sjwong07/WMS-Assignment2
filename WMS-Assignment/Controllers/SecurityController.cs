using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS_Assignment.Models;
using System.Text.RegularExpressions;

namespace WMS_Assignment.Controllers;

public class SecurityController : Controller
{
    private readonly DB Db;

    public SecurityController(DB db)
    {
        Db = db;
    }

    //---------------- Register ----------------

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(User model)
    {
        if (Db.Users.Any(x => x.Username == model.Username))
        {
            ViewBag.Error = "Username already exists.";
            return View(model);
        }

        if (Db.Users.Any(x => x.Email == model.Email))
        {
            ViewBag.Error = "Email already exists.";
            return View(model);
        }

        //Password validation

        string pattern =
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&^#()_+\-=\[\]{};':""\\|,.<>\/?]).{8,}$";

        if (!Regex.IsMatch(model.Password, pattern))
        {
            ViewBag.Error =
                "Password must contain at least 8 characters, 1 uppercase, 1 lowercase, 1 number and 1 special character.";

            return View(model);
        }

        Db.Users.Add(model);
        Db.SaveChanges();

        TempData["Success"] = "Account created successfully.";

        return RedirectToAction("Login");
    }

    //---------------- Login ----------------

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        var user = Db.Users.FirstOrDefault(x => x.Username == username);

        if (user == null)
        {
            ViewBag.Error = "Invalid username or password.";
            return View();
        }

        if (user.LockoutEnd != null &&
            user.LockoutEnd > DateTime.Now)
        {
            ViewBag.Error =
                $"Account locked. Try again after {user.LockoutEnd}";
            return View();
        }

        if (user.Password != password)
        {
            user.FailedLogin++;

            if (user.FailedLogin >= 3)
            {
                user.LockoutEnd = DateTime.Now.AddMinutes(3);
                user.FailedLogin = 0;

                ViewBag.Error =
                    "Too many failed attempts. Please wait 3 minutes.";
            }
            else
            {
                ViewBag.Error =
                    $"Invalid password. Remaining attempts: {3 - user.FailedLogin}";
            }

            Db.SaveChanges();

            return View();
        }

        //Success

        user.FailedLogin = 0;
        user.LockoutEnd = null;

        Db.SaveChanges();

        HttpContext.Session.SetString("User", user.Username);

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        return RedirectToAction("Login");
    }
}