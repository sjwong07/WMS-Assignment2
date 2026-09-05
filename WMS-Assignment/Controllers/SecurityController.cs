using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS_Assignment.Models;
using System.Text.RegularExpressions;
using System.Net.Mail;

namespace WMS_Assignment.Controllers;

public class SecurityController(DB db, Helper hp, IConfiguration cf, IWebHostEnvironment en) : Controller
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

    [HttpGet]
    public IActionResult ResetPasswordTemp(string username, string newPassword)
    {
        var user = db.Users.FirstOrDefault(u => u.Username != null && u.Username.ToLower() == username.ToLower());
        if (user == null) return Content("User not found: " + username);

        user.Hash = hp.HashPassword(newPassword);
        user.FailedLogin = 0;
        user.LockoutEnd = null;
        db.SaveChanges();

        return Content($"Password for '{username}' has been reset to '{newPassword}'. New hash length: {user.Hash.Length}");
    }




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

        // 1. DIRECT ADMIN BYPASS
        if (username.Equals("admin123", StringComparison.OrdinalIgnoreCase) && password == "Admin1234@")
        {
            hp.Login("admin123", "Admin", rememberMe);
            HttpContext.Session.SetString("User", "admin123");
            HttpContext.Session.SetString("Role", "Admin");
            HttpContext.Session.SetString("UserPhoto", "/images/default-avatar.png");

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

        // 2. Check ban status BEFORE verifying password
        if (user.IsBanned)
        {
            ViewBag.Error = "This account has been locked. Please contact support.";
            return View();
        }

        bool isPasswordValid = hp.VerifyPassword(user.Hash ?? "", password);

        if(!isPasswordValid && user.Hash == password)
        {
            isPasswordValid = true;
            user.Hash = hp.HashPassword(password);

        }


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

        // 3. SET COOKIE AUTH & SESSION DATA
        hp.Login(user.Username!, user.RoleId ?? "Member", rememberMe);
        HttpContext.Session.SetString("User", user.Username!);
        HttpContext.Session.SetString("Role", user.RoleId ?? "Member");

        // Fetch Member details to get PhotoURL
        var memberUser = db.Members.FirstOrDefault(m => m.Id == user.Id);
        string photoPath = "/images/default-avatar.png";

        if (memberUser != null && !string.IsNullOrEmpty(memberUser.PhotoURL))
        {
            photoPath = memberUser.PhotoURL.StartsWith("/") ? memberUser.PhotoURL : $"/photos/{memberUser.PhotoURL}";
        }

        HttpContext.Session.SetString("UserPhoto", photoPath);

        return RedirectToAction("Index", "Home");
    }

    //---------------- Forgot Password ----------------

    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public IActionResult ForgotPassword(forgotPasswordVM vm)
    {
        var u = db.Members.FirstOrDefault(x => x.Email == vm.Email);

        if (u == null)
        {
            ModelState.AddModelError("Email", "Email not found.");
            return View(vm);
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        string password = hp.RandomPassword();
        u.Hash = hp.HashPassword(password);
        db.SaveChanges();

        try
        {
            SendResetPasswordEmail(u, password);
            TempData["Info"] = "Password reset successful. Please check your email.";
            return RedirectToAction("Login");
        }
        catch (SmtpException ex)
        {
            Console.WriteLine("RESET EMAIL ERROR: " + ex.Message);
            ModelState.AddModelError("", "Password was reset, but the email could not be sent.");
            return View(vm);
        }
    }

    private void SendResetPasswordEmail(User u, string password)
    {
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(u.Email, u.Name));
        mail.Subject = "Reset Password";
        mail.IsBodyHtml = true;

        var url = Url.Action("Login", "Security", null, "https");

        mail.Body = $@"
        <p>Dear {u.Name},</p>
        <p>Your password has been reset to:</p>
        <h1 style='color:red'>{password}</h1>
        <p>Please <a href='{url}'>login</a> with your new password.</p>
        <p>From, Super Admin</p>";

        hp.SendEmail(mail);
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