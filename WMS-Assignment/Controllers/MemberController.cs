using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS_Assignment.Models;

namespace WMS_Assignment.Controllers;

[Authorize]
public class MemberController(DB db, Helper hp) : Controller
{
    // GET: /Member/Dashboard
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Dashboard()
    {
        var username = User.Identity?.Name?.Trim();
        if (string.IsNullOrEmpty(username))
        {
            return RedirectToAction("Login", "Security");
        }

        var member = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (member == null)
        {
            return RedirectToAction("Index", "Home");
        }

        // Fetch order history for the logged-in member
        var memberOrders = await db.Orders
            .Where(o => o.UserId == member.Id)
            .ToListAsync();

        ViewBag.TotalOrders = memberOrders.Count;
        ViewBag.ActiveOrders = memberOrders.Count(o => o.Status == "Pending" || o.Status == "Preparing");
        ViewBag.TotalSpent = memberOrders.Where(o => o.PaymentStatus == "Paid").Sum(o => o.TotalAmount);

        return View(memberOrders);
    }

    // GET: /Member/Profile
    public async Task<IActionResult> Profile()
    {
        var username = User.Identity?.Name?.Trim();
        if (string.IsNullOrEmpty(username))
        {
            return RedirectToAction("Login", "Security");
        }

        var member = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (member == null)
        {
            return RedirectToAction("Index", "Home");
        }

        var vm = new UpdateProfileVM
        {
            Username = member.Username,
            Email = member.Email,
            CurrentPhotoURL = member.PhotoURL
        };

        return View(vm);
    }

    // POST: /Member/Profile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UpdateProfileVM vm, string? croppedImageBase64)
    {
        var currentUsername = User.Identity?.Name?.Trim();
        var member = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == currentUsername!.ToLower());

        if (member == null)
        {
            return RedirectToAction("Login", "Security");
        }

        // Check if new username is already taken by someone else
        if (vm.Username.Trim().ToLower() != member.Username.ToLower() &&
            await db.Users.AnyAsync(u => u.Username.ToLower() == vm.Username.Trim().ToLower()))
        {
            ModelState.AddModelError("Username", "This username is already taken.");
        }

        // Check if new email is already taken by someone else
        if (vm.Email.Trim().ToLower() != member.Email.ToLower() &&
            await db.Users.AnyAsync(u => u.Email.ToLower() == vm.Email.Trim().ToLower()))
        {
            ModelState.AddModelError("Email", "This email address is already in use.");
        }

        // Validate standard file upload if used directly without cropper
        if (vm.Photo != null && vm.Photo.Length > 0)
        {
            var photoError = hp.ValidatePhoto(vm.Photo);
            if (!string.IsNullOrEmpty(photoError))
            {
                ModelState.AddModelError("Photo", photoError);
            }
        }

        if (ModelState.IsValid)
        {
            member.Username = vm.Username.Trim();
            member.Name = vm.Username.Trim();
            member.Email = vm.Email.Trim();

            // Handle Cropped Base64 Image if submitted
            if (!string.IsNullOrEmpty(croppedImageBase64))
            {
                try
                {
                    var base64Data = croppedImageBase64.Substring(croppedImageBase64.IndexOf(",") + 1);
                    byte[] imageBytes = Convert.FromBase64String(base64Data);

                    string fileName = "profile_" + Guid.NewGuid().ToString("N")[..8] + ".jpg";
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "photos", fileName);

                    if (!string.IsNullOrEmpty(member.PhotoURL))
                    {
                        hp.DeletePhoto(member.PhotoURL, "photos");
                    }

                    await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                    member.PhotoURL = "photos/" + fileName;
                }
                catch
                {
                    ModelState.AddModelError("Photo", "Failed to process cropped image.");
                    vm.CurrentPhotoURL = member.PhotoURL;
                    return View(vm);
                }
            }
            // Fallback to standard file upload if no crop data exists
            else if (vm.Photo != null && vm.Photo.Length > 0)
            {
                if (!string.IsNullOrEmpty(member.PhotoURL))
                {
                    hp.DeletePhoto(member.PhotoURL, "photos");
                }
                member.PhotoURL = hp.SavePhoto(vm.Photo, "photos");
            }

            await db.SaveChangesAsync();

            // Re-issue the auth cookie so navbar (username + photo) reflects changes immediately
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, member.Username ?? ""),
                new Claim(ClaimTypes.NameIdentifier, member.Id ?? ""),
                new Claim(ClaimTypes.Role, member.RoleId ?? "Member"),
                new Claim("PhotoURL", member.PhotoURL ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

            TempData["Success"] = "Profile updated successfully!";

            vm.CurrentPhotoURL = member.PhotoURL;
            return RedirectToAction("Profile");
        }

        vm.CurrentPhotoURL = member.PhotoURL;
        return View(vm);
    }

    public IActionResult UpdatePassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePassword(UpdatePasswordVM vm)
    {
        var username = User.Identity!.Name?.Trim();
        var member = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username!.ToLower());
        if (member == null)
        {
            return RedirectToAction("Index");
        }

        if (!hp.VerifyPassword(member.Hash, vm.CurrentPassword))
        {
            ModelState.AddModelError("Current", "Current Password Not Matched");
        }

        if (ModelState.IsValid)
        {
            member.Hash = hp.HashPassword(vm.NewPassword);
            await db.SaveChangesAsync();

            TempData["Info"] = "Password Updated.";
            return RedirectToAction();
        }
        return View();
    }
}