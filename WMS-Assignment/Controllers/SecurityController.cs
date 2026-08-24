using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS_Assignment.Models;
using System.Text.RegularExpressions;
using System.Net.Mail;


namespace WMS_Assignment.Controllers;

public class SecurityController(DB db,Helper hp,
                            IConfiguration cf, IWebHostEnvironment en) : Controller
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
    public IActionResult forgotPassword(forgotPasswordVM vm)
    {
        var u = db.Users.FirstOrDefault(u => u.Email == vm.Email);

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

            TempData["Info"] =
                "Password reset successful. Please check your email.";

            return RedirectToAction("Login");
        }
        catch (SmtpException ex)
        {
            Console.WriteLine("RESET EMAIL ERROR: " + ex.Message);

            ModelState.AddModelError(
                "",
                "Password was reset, but the email could not be sent."
            );

            return View(vm);
        }
    }

    private void SendResetPasswordEmail(User u, string password)
    {
        Console.WriteLine("START SENDING EMAIL");
        var mail = new MailMessage();

        mail.To.Add(new MailAddress(u.Email, u.Name));
        mail.Subject = "Reset Password";
        mail.IsBodyHtml = true;

        var url = Url.Action(
            "Login",
            "Security",
            null,
            "https"
        );

        mail.Body = $@"
        <p>Dear {u.Name},</p>

        <p>Your password has been reset to:</p>

        <h1 style='color:red'>{password}</h1>

        <p>
            Please <a href='{url}'>login</a>
            with your new password.
        </p>

        <p>From, Super Admin</p>
    ";

        hp.SendEmail(mail);
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        return RedirectToAction("Login");
    }





}