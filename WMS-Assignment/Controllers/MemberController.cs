using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WMS_Assignment.Models;

namespace WMS_Assignment.Controllers;

[Authorize]
public class MemberController(DB db, Helper hp) : Controller
{
    // GET: /Member/Profile
    public IActionResult Profile()
    {
        var username = User.Identity?.Name?.Trim();
        if (string.IsNullOrEmpty(username))
        {
            return RedirectToAction("Login", "Security");
        }

        var member = db.Members.FirstOrDefault(m => m.Username.ToLower() == username.ToLower());
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
    public async Task<IActionResult> Profile(UpdateProfileVM vm)
    {
        var currentUsername = User.Identity?.Name?.Trim();
        var member = db.Members.FirstOrDefault(m => m.Username.ToLower() == currentUsername!.ToLower());

        if (member == null)
        {
            return RedirectToAction("Login", "Security");
        }

        // Check if new username is already taken by someone else
        if (vm.Username.Trim().ToLower() != member.Username.ToLower() &&
            db.Members.Any(m => m.Username.ToLower() == vm.Username.Trim().ToLower()))
        {
            ModelState.AddModelError("Username", "This username is already taken.");
        }

        // Check if new email is already taken by someone else
        if (vm.Email.Trim().ToLower() != member.Email.ToLower() &&
            db.Members.Any(m => m.Email.ToLower() == vm.Email.Trim().ToLower()))
        {
            ModelState.AddModelError("Email", "This email address is already in use.");
        }

        if (ModelState.IsValid)
        {
            member.Username = vm.Username.Trim();
            member.Name = vm.Username.Trim();
            member.Email = vm.Email.Trim();

            // Save new photo if uploaded
            if (vm.Photo != null && vm.Photo.Length > 0)
            {
                if (!string.IsNullOrEmpty(member.PhotoURL))
                {
                    hp.DeletePhoto(member.PhotoURL, "photos");
                }
                member.PhotoURL = hp.SavePhoto(vm.Photo, "photos");
            }

            db.SaveChanges();

            // Re-issue the auth cookie so navbar (username + photo) reflects changes immediately
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, member.Username ?? ""),
                new Claim(ClaimTypes.NameIdentifier, member.Id ?? ""),
                new Claim(ClaimTypes.Role, member.RoleId ?? "Member"),
                new Claim("PhotoURL", member.PhotoURL ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity));

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        vm.CurrentPhotoURL = member.PhotoURL;
        return View(vm);
    }
}