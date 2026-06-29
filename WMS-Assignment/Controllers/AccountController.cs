using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS_Assignment.Data;
using WMS_Assignment.Models.Entities;
using WMS_Assignment.Models.ViewModels.Account;

namespace WMS_Assignment.Controllers
{
    public class AccountController : Controller
    {
        private readonly DB _context;

        public AccountController(DB context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null && user.IsActive && !user.IsLockedOut)
                {
                    // Check if account is locked
                    if (user.LockoutEndDate.HasValue && user.LockoutEndDate.Value > DateTime.Now)
                    {
                        ModelState.AddModelError(string.Empty, "Account is temporarily locked. Please try again later.");
                        return View(model);
                    }

                    // Check password (simplified - use proper hashing in production)
                    if (user.PasswordHash == model.Password)
                    {
                        // Reset failed login attempts
                        user.FailedLoginAttempts = 0;
                        user.IsLockedOut = false;
                        user.LastLoginDate = DateTime.Now;
                        await _context.SaveChangesAsync();

                        // Create claims
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                            new Claim(ClaimTypes.Email, user.Email),
                            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                            new Claim(ClaimTypes.Role, user.Role.RoleName)
                        };

                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                            return Redirect(returnUrl);

                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        // Increment failed login attempts
                        user.FailedLoginAttempts++;
                        if (user.FailedLoginAttempts >= 3)
                        {
                            user.IsLockedOut = true;
                            user.LockoutEndDate = DateTime.Now.AddMinutes(15);
                            await _context.SaveChangesAsync();
                            ModelState.AddModelError(string.Empty, "Account locked due to multiple failed attempts. Please try again after 15 minutes.");
                        }
                        else
                        {
                            await _context.SaveChangesAsync();
                            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt or account inactive.");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email already registered.");
                    return View(model);
                }

                var user = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PasswordHash = model.Password, // In production, hash the password
                    PhoneNumber = model.PhoneNumber,
                    RoleId = 3, // Customer role
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Auto-login after registration
                var role = await _context.Roles.FindAsync(3);
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                    new Claim(ClaimTypes.Role, role.RoleName)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}