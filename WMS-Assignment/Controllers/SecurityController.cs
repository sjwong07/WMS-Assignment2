using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS_Assignment.Models;

namespace WMS_Assignment.Controllers;

public class SecurityController(DB db, Helper hp) : Controller
{
    //---------------- Register ----------------

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(RegisterVM vm)
    {
        // Check duplicates across all users
        if (db.Users.Any(u => u.Username != null && u.Username.ToLower() == vm.Username.Trim().ToLower()))
        {
            ModelState.AddModelError("Username", "Username is already taken.");
        }
        if (db.Users.Any(u => u.Email != null && u.Email.ToLower() == vm.Email.Trim().ToLower()))
        {
            ModelState.AddModelError("Email", "Email address is already registered.");
        }

        if (ModelState.IsValid)
        {
            var member = new Member
            {
                Id = Guid.NewGuid().ToString(),
                Username = vm.Username.Trim(),
                Name = $"{vm.FirstName} {vm.LastName}".Trim(),
                Email = vm.Email.Trim(),
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Hash = hp.HashPassword(vm.Password),
                RoleId = "Member",
                CreatedDate = DateTime.Now
            };

            // Save photo if uploaded
            if (vm.ProfilePhoto != null && vm.ProfilePhoto.Length > 0)
            {
                member.PhotoURL = hp.SavePhoto(vm.ProfilePhoto, "photos");
            }

            db.Members.Add(member);
            db.SaveChanges();

            TempData["Success"] = "Account created successfully. Please log in.";
            return RedirectToAction("Login", "Security");
        }

        return View(vm);
    }

    //---------------- Login ----------------

    //---------------- Login ----------------

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string Username, string Password, bool rememberMe = false)
    {
        var username = Username?.Trim() ?? "";
        var password = Password?.Trim() ?? "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ViewBag.Error = "Please enter both username and password.";
            return View();
        }

        // 1. DIRECT ADMIN BYPASS (Always logs in regardless of DB state)
        if (username.Equals("admin123", StringComparison.OrdinalIgnoreCase) && password == "Admin1234@")
        {
            hp.Login("admin123", "Admin", rememberMe);
            HttpContext.Session.SetString("User", "admin123");
            HttpContext.Session.SetString("Role", "Admin");

            // If you have an AdminController, redirect there; otherwise Home
            return RedirectToAction("Index", "Home");
        }

        // 2. NORMAL MEMBER DATABASE AUTHENTICATION
        var user = db.Users.FirstOrDefault(x => x.Username != null && x.Username.ToLower() == username.ToLower());

        if (user == null)
        {
            ViewBag.Error = "Invalid username or password.";
            return View();
        }

        if (user.LockoutEnd != null && user.LockoutEnd > DateTime.Now)
        {
            ViewBag.Error = $"Account locked. Try again after {user.LockoutEnd}";
            return View();
        }

        bool isPasswordValid = hp.VerifyPassword(user.Hash ?? "", password);

        if (!isPasswordValid)
        {
            user.FailedLogin++;
            if (user.FailedLogin >= 3)
            {
                user.LockoutEnd = DateTime.Now.AddMinutes(5);
                user.FailedLogin = 0;
                ViewBag.Error = "Too many failed attempts. Please wait 5 minutes.";
            }
            else
            {
                ViewBag.Error = $"Invalid username or password. Remaining attempts: {3 - user.FailedLogin}";
            }
            db.SaveChanges();
            return View();
        }

        user.FailedLogin = 0;
        user.LockoutEnd = null;
        db.SaveChanges();

        hp.Login(user.Username!, user.RoleId ?? "Member", rememberMe);
        HttpContext.Session.SetString("User", user.Username!);
        HttpContext.Session.SetString("Role", user.RoleId ?? "Member");

        if (user.RoleId == "Admin")
        {
            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction("Index", "Home");
    }
    //---------------- Forgot Password ----------------

    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public IActionResult ForgotPassword(string Email)
    {
        var u = db.Members.FirstOrDefault(x => x.Email.ToLower() == Email.Trim().ToLower());

        if (u == null)
        {
            ModelState.AddModelError("Email", "Email not found.");
        }

        if (ModelState.IsValid && u != null)
        {
            string password = hp.RandomPassword();
            u.Hash = hp.HashPassword(password);
            db.SaveChanges();

            TempData["Info"] = $"Password reset to <b>{password}</b>.";
            return RedirectToAction("Login");
        }

        return View();
    }

    //---------------- Logout ----------------

    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync("CookieAuth");

        TempData["Success"] = "You have been logged out successfully.";
        return RedirectToAction("Login", "Security");
    }
}