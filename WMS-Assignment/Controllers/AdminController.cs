using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS_Assignment.Models;

namespace WMS_Assignment.Controllers;

[Authorize]
public class AdminController(DB db, Helper hp) : Controller
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Dashboard()
    {
        // Gather key admin metrics
        ViewBag.TotalMenuItems = await db.MenuItems.CountAsync();

        // Check if Orders table exists in your context and count active/pending orders
        var orders = await db.Orders.ToListAsync();
        ViewBag.TotalOrders = orders.Count;
        ViewBag.ActiveOrders = orders.Count(o => o.Status == "Pending" || o.Status == "Preparing");
        ViewBag.TotalRevenue = orders.Where(o => o.PaymentStatus == "Paid").Sum(o => o.TotalAmount);

        ViewBag.TotalMembers = await db.Users.OfType<Member>().CountAsync();

        return View();
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Profile()
    {
        var username = User.Identity?.Name?.Trim();
        if (string.IsNullOrEmpty(username))
        {
            return RedirectToAction("Login", "Security");
        }

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (admin == null)
        {
            return RedirectToAction("Index", "Home");
        }

        // Refresh authentication cookie claim if PhotoURL claim is missing or outdated
        var currentClaimPhoto = User.FindFirst("PhotoURL")?.Value;
        if (!string.IsNullOrEmpty(admin.PhotoURL) && currentClaimPhoto != admin.PhotoURL)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, admin.Username ?? ""),
                new Claim(ClaimTypes.NameIdentifier, admin.Id ?? ""),
                new Claim(ClaimTypes.Role, admin.RoleId ?? "Admin"),
                new Claim("PhotoURL", admin.PhotoURL ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), new AuthenticationProperties { IsPersistent = true });

            HttpContext.Session.SetString("User", admin.Username ?? "");
            HttpContext.Session.SetString("UserPhoto", admin.PhotoURL ?? "");
        }

        var vm = new UpdateProfileVM
        {
            Username = admin.Username,
            Email = admin.Email,
            CurrentPhotoURL = admin.PhotoURL
        };

        return View("~/Views/Member/Profile.cshtml", vm);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UpdateProfileVM vm, string? croppedImageBase64)
    {
        var currentUsername = User.Identity?.Name?.Trim();
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == currentUsername!.ToLower());

        if (admin == null)
        {
            return RedirectToAction("Login", "Security");
        }

        if (vm.Username.Trim().ToLower() != admin.Username.ToLower() &&
            await db.Users.AnyAsync(u => u.Username.ToLower() == vm.Username.Trim().ToLower()))
        {
            ModelState.AddModelError("Username", "This username is already taken.");
        }

        if (vm.Email.Trim().ToLower() != admin.Email.ToLower() &&
            await db.Users.AnyAsync(u => u.Email.ToLower() == vm.Email.Trim().ToLower()))
        {
            ModelState.AddModelError("Email", "This email address is already in use.");
        }

        if (ModelState.IsValid)
        {
            admin.Username = vm.Username.Trim();
            admin.Name = vm.Username.Trim();
            admin.Email = vm.Email.Trim();

            if (!string.IsNullOrEmpty(croppedImageBase64))
            {
                try
                {
                    var base64Data = croppedImageBase64.Substring(croppedImageBase64.IndexOf(",") + 1);
                    byte[] imageBytes = Convert.FromBase64String(base64Data);

                    string fileName = "profile_" + Guid.NewGuid().ToString("N")[..8] + ".jpg";
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "photos", fileName);

                    if (!string.IsNullOrEmpty(admin.PhotoURL))
                    {
                        hp.DeletePhoto(admin.PhotoURL, "photos");
                    }

                    await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                    admin.PhotoURL = "photos/" + fileName;
                }
                catch
                {
                    ModelState.AddModelError("Photo", "Failed to process cropped image.");
                    vm.CurrentPhotoURL = admin.PhotoURL;
                    return View("~/Views/Member/Profile.cshtml", vm);
                }
            }
            else if (vm.Photo != null && vm.Photo.Length > 0)
            {
                if (!string.IsNullOrEmpty(admin.PhotoURL))
                {
                    hp.DeletePhoto(admin.PhotoURL, "photos");
                }
                admin.PhotoURL = hp.SavePhoto(vm.Photo, "photos");
            }

            await db.SaveChangesAsync();

            // Re-issue cookie claims with the updated photo
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, admin.Username ?? ""),
                new Claim(ClaimTypes.NameIdentifier, admin.Id ?? ""),
                new Claim(ClaimTypes.Role, admin.RoleId ?? "Admin"),
                new Claim("PhotoURL", admin.PhotoURL ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), new AuthenticationProperties { IsPersistent = true });

            HttpContext.Session.SetString("User", admin.Username ?? "");
            if (!string.IsNullOrEmpty(admin.PhotoURL))
            {
                HttpContext.Session.SetString("UserPhoto", admin.PhotoURL);
            }

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        vm.CurrentPhotoURL = admin.PhotoURL;
        return View("~/Views/Member/Profile.cshtml", vm);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public IActionResult AdminMenu()
    {
        var m = db.MenuItems.Include(x => x.Category).Include(x => x.Photos).ToList();
        return View(m);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public IActionResult Create()
    {
        var vm = new ProductInsertVM
        {
            FoodCategories = db.FoodCategories.ToList()
        };
        return View(vm);
    }

    [HttpPost]
    public IActionResult Create(ProductInsertVM vm)
    {
        if (!ModelState.IsValid)
        {
            vm.FoodCategories = db.FoodCategories.ToList();
            return View(vm);
        }

        var item = new MenuItem
        {
            Id = GenerateNextId(),
            Name = vm.Name,
            Description = vm.description,
            Price = decimal.Parse(vm.Price),
            CategoryId = vm.CategoryId,
        };

        if (vm.Photos != null)
        {
            foreach (var photo in vm.Photos)
            {
                if (photo == null || photo.Length == 0) continue;

                var error = hp.ValidatePhoto(photo);

                if (!string.IsNullOrEmpty(error))
                {
                    ModelState.AddModelError("Photos", error);
                    vm.FoodCategories = db.FoodCategories.ToList();
                    return View(vm);
                }

                var url = hp.SavePhoto(photo, "Menu");

                item.Photos.Add(new MenuItemPhoto { PhotoURL = url });
            }
        }

        db.MenuItems.Add(item);
        db.SaveChanges();

        TempData["Info"] = "Item created successfully.";
        return RedirectToAction("AdminMenu");
    }

    private string GenerateNextId()
    {
        var maxNum = db.MenuItems
            .Where(x => x.Id.StartsWith("P"))
            .Select(x => x.Id.Substring(1))
            .AsEnumerable()
            .Select(s => int.TryParse(s, out int n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        int next = maxNum + 1;
        return "P" + next.ToString("000");
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public IActionResult Update(string id)
    {
        var item = db.MenuItems.Include(x => x.Photos).FirstOrDefault(x => x.Id == id);
        if (item == null) return RedirectToAction("AdminMenu");

        var vm = new ProductUpdateVM
        {
            Id = item.Id,
            Name = item.Name,
            description = item.Description,
            Price = item.Price.ToString("0.00"),
            CategoryId = item.CategoryId,
            CurrentPhotoURL = item.Photos,
            FoodCategories = db.FoodCategories.ToList(),
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult Update(string id, ProductUpdateVM vm)
    {
        var item = db.MenuItems.Include(x => x.Photos).FirstOrDefault(x => x.Id == id);
        if (item == null) return RedirectToAction("AdminMenu");

        if (!ModelState.IsValid)
        {
            vm.FoodCategories = db.FoodCategories.ToList();
            vm.CurrentPhotoURL = item.Photos;
            return View(vm);
        }

        item.Name = vm.Name;
        item.Description = vm.description;
        item.Price = decimal.Parse(vm.Price);
        item.CategoryId = vm.CategoryId;

        if (vm.Photos != null)
        {
            foreach (var photo in vm.Photos)
            {
                if (photo == null || photo.Length == 0) continue;

                var error = hp.ValidatePhoto(photo);
                if (!string.IsNullOrEmpty(error))
                {
                    ModelState.AddModelError("Photos", error);
                    vm.FoodCategories = db.FoodCategories.ToList();
                    vm.CurrentPhotoURL = item.Photos;
                    return View(vm);
                }

                var url = hp.SavePhoto(photo, "Menu");
                item.Photos.Add(new MenuItemPhoto { PhotoURL = url });
            }
        }

        db.SaveChanges();

        TempData["Info"] = "Item updated successfully.";
        return RedirectToAction("AdminMenu");
    }

    [HttpPost]
    public IActionResult DeletePhoto(int photoId, string menuItemId)
    {
        var photo = db.MenuItemPhotos.Find(photoId);
        if (photo != null)
        {
            hp.DeletePhoto(photo.PhotoURL, "");
            db.MenuItemPhotos.Remove(photo);
            db.SaveChanges();
        }
        return RedirectToAction("Update", new { id = menuItemId });
    }

    [HttpPost]
    public IActionResult Delete(string id)
    {
        var item = db.MenuItems.Include(x => x.Photos).FirstOrDefault(x => x.Id == id);
        if (item != null)
        {
            foreach (var photo in item.Photos)
            {
                hp.DeletePhoto(photo.PhotoURL, "");
            }
            db.MenuItems.Remove(item);
            db.SaveChanges();
            TempData["Info"] = "Item deleted successfully.";
        }
        return RedirectToAction("AdminMenu");
    }

    [Authorize(Roles = "SuperAdmin")]
    public IActionResult AdminList()
    {
        var admins = db.Users.OfType<Admin>().Select(a => new AdminListVM
        {
            Id = a.Id,
            Username = a.Username,
            Email = a.Email,
            CreateDate = a.CreatedDate,
            IsBanned = a.IsBanned
        })
        .ToList();

        return View(admins);
    }

    [Authorize(Roles = "SuperAdmin")]
    public IActionResult BanAdmin(string id)
    {
        var admin = db.Users.OfType<Admin>().FirstOrDefault(a => a.Id == id);
        if (admin == null)
            return NotFound();

        var vm = new BanVM
        {
            Id = admin.Id,
            Username = admin.Username
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult BanAdmin(BanVM vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var admin = db.Users.OfType<Admin>().FirstOrDefault(a => a.Id == vm.Id);
        if (admin == null)
            return NotFound();

        admin.IsBanned = true;
        admin.BanReason = vm.BanReason;

        db.BannedUsers.Add(new BannedUser
        {
            Id = hp.GenerateNextBannedId(),
            UserId = admin.Id,
            Reason = vm.BanReason,
            BannedDate = DateTime.Now
        });

        db.SaveChanges();

        return RedirectToAction("AdminList");
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public IActionResult MemberList()
    {
        var members = db.Users.OfType<Member>().Select(u => new MemberListVM
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            CreateDate = u.CreatedDate,
            IsBanned = u.IsBanned
        })
       .ToList();
        return View(members);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public IActionResult BanMember(string id)
    {
        var member = db.Users.OfType<Member>().FirstOrDefault(m => m.Id == id);
        if (member == null)
            return NotFound();

        var vm = new BanVM
        {
            Id = member.Id,
            Username = member.Username
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult BanMember(BanVM vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var member = db.Users.OfType<Member>().FirstOrDefault(m => m.Id == vm.Id);
        if (member == null)
            return NotFound();

        member.IsBanned = true;
        member.BanReason = vm.BanReason;

        db.BannedUsers.Add(new BannedUser
        {
            Id = hp.GenerateNextBannedId(),
            UserId = member.Id,
            Reason = vm.BanReason,
            BannedDate = DateTime.Now
        });

        db.SaveChanges();

        return RedirectToAction("MemberList");
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public IActionResult BannedUser()
    {
        var records = db.BannedUsers
            .Include(b => b.User)
            .OrderByDescending(b => b.BannedDate)
            .ToList();

        return View(records);
    }

    [HttpPost]
    public IActionResult UnbanAdmin(string id)
    {
        var admin = db.Users.OfType<Admin>().FirstOrDefault(a => a.Id == id);
        if (admin == null) return NotFound();

        admin.IsBanned = false;
        admin.BanReason = null;

        var record = db.BannedUsers.FirstOrDefault(b => b.UserId == id);
        if (record != null)
            db.BannedUsers.Remove(record);

        db.SaveChanges();
        return RedirectToAction("AdminList");
    }

    [HttpPost]
    public IActionResult UnbanMember(string id)
    {
        var member = db.Users.OfType<Member>().FirstOrDefault(m => m.Id == id);
        if (member == null) return NotFound();

        member.IsBanned = false;
        member.BanReason = null;

        var record = db.BannedUsers.FirstOrDefault(b => b.UserId == id);
        if (record != null)
            db.BannedUsers.Remove(record);

        db.SaveChanges();
        return RedirectToAction("MemberList");
    }

    [HttpPost]
    public IActionResult ClearAllRecord()
    {
        var all = db.BannedUsers.ToList();
        db.BannedUsers.RemoveRange(all);
        db.SaveChanges();

        TempData["Info"] = "All Record Cleared";
        return RedirectToAction("BannedUser");
    }

    public IActionResult RegisterAdmin()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RegisterAdmin(RegisterAdminVM vm)
    {
        if (db.Users.Any(u => u.Username.ToLower() == vm.Username.Trim().ToLower()))
        {
            ModelState.AddModelError("Username", "This username is already taken.");
        }

        if (db.Users.Any(u => u.Email.ToLower() == vm.Email.Trim().ToLower()))
        {
            ModelState.AddModelError("Email", "This email address is already in use.");
        }

        if (ModelState.IsValid)
        {
            var admin = new Admin
            {
                Id = Guid.NewGuid().ToString(),
                Username = vm.Username.Trim(),
                Name = vm.Username.Trim(),
                Email = vm.Email.Trim(),
                Hash = hp.HashPassword(vm.Password),
                RoleId = "Admin",
                CreatedDate = DateTime.Now
            };

            db.Users.Add(admin);
            db.SaveChanges();

            TempData["Info"] = "Admin created successfully.";
            return RedirectToAction("AdminList");
        }

        return View(vm);
    }
}