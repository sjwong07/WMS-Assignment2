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
    public IActionResult Register(RegisterVM vm)
    {
        if (ModelState.IsValid)
        {
            db.Members.Add(new()
            {
                Id = Guid.NewGuid().ToString("N"),
                Username = vm.Username,
                Name = vm.FirstName + "  " + vm.LastName,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Email = vm.Email,
                Hash = hp.HashPassword(vm.Password),
                PhotoURL = hp.SavePhoto(vm.ProfilePhoto, "photos"),

            });
            db.SaveChanges();

            TempData["Info"] = "Register Successful,Please Login";
            return RedirectToAction("Login");
        }

    
        if (db.Users.Any(u => u.Username == vm.Username))
        {
            ViewBag.Message = "Username already exists.";
            return View("~/Views/Security/Register.cshtml");
        }


        return View(vm);
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