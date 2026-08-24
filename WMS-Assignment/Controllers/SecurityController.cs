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
        // Check duplicates in db.Members
        if (db.Members.Any(u => u.Username.ToLower() == vm.Username.Trim().ToLower()))
        {
            ModelState.AddModelError("Username", "Username is already taken.");
        }
        if (db.Members.Any(u => u.Email.ToLower() == vm.Email.Trim().ToLower()))
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

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(LoginVM vm, bool rememberMe = false)
    {
        var user = db.Members.FirstOrDefault(x => x.Username.ToLower() == vm.Username.Trim().ToLower());

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

        // Verify the entered password against the stored Hash
        bool isPasswordValid = hp.VerifyPassword(user.Hash, vm.Password);

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
                ViewBag.Error = $"Invalid password. Remaining attempts: {3 - user.FailedLogin}";
            }

            db.SaveChanges();
            return View();
        }

        // Success: Reset failed attempts & lockout
        user.FailedLogin = 0;
        user.LockoutEnd = null;
        db.SaveChanges();

        // Sign in via Cookie Auth helper
        hp.Login(user.Username, vm.Password, rememberMe);

        // Store session backup
        HttpContext.Session.SetString("User", user.Username);

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
        // 1. Clear session
        HttpContext.Session.Clear();

        // 2. Clear authentication cookie
        await HttpContext.SignOutAsync("CookieAuth");

        TempData["Success"] = "You have been logged out successfully.";
        return RedirectToAction("Login", "Security");
    }
}