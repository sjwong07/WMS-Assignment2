using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS_Assignment.Models;
using System.Text.RegularExpressions;

namespace WMS_Assignment.Controllers;

public class SecurityController(DB db,Helper hp) : Controller
{

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(User model)
    {
        if (db.Users.Any(x => x.Username == model.Username))
        {
            ViewBag.Error = "Username already exists.";
            return View(model);
        }

        if (db.Users.Any(x => x.Email == model.Email))
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

        db.Users.Add(model);
        db.SaveChanges();

        TempData["Success"] = "Account created successfully.";

        return RedirectToAction("Login");
    }

    //---------------- Login ----------------

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(LoginVM vm)

    {
        
        var user = db.Users.FirstOrDefault(x => x.Username == vm.Username);

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

        if (user.Password != vm.Password)
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

            db.SaveChanges();

            return View();
        }

        //Success

        user.FailedLogin = 0;
        user.LockoutEnd = null;

        db.SaveChanges();

        HttpContext.Session.SetString("User", user.Username);

        return RedirectToAction("Index", "Home");
    }

    public  IActionResult forgotPassword (){

        return View();
    }

    [HttpPost]
    public IActionResult forgotPassword(string Email)
    {
        var u = db.Users.Find(Email);

        if (u == null)
        {
            ModelState.AddModelError("Email", "Email not found.");
        }


        if (ModelState.IsValid)
        {
            string password = hp.RandomPassword();
            u!.Hash = hp.HashPassword(password);
            db.SaveChanges();

            TempData["Info"] = $"Password reset to <b>{password}</b>.";
        }
        return RedirectToAction("Login");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        return RedirectToAction("Login");
    }





}