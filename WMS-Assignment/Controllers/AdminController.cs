using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS_Assignment.Models;

namespace WMS_Assignment.Controllers;

[Authorize]
public class AdminController(DB db, Helper hp) : Controller
{
    public IActionResult AdminMenu()
    {
        var m = db.MenuItems.Include(x => x.Category).Include(x => x.Photos).ToList();
        return View(m);
    }

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
        var lastId = db.MenuItems
            .OrderByDescending(x => x.Id)
            .Select(x => x.Id)
            .FirstOrDefault();

        int next = 1;
        if (!string.IsNullOrEmpty(lastId) && int.TryParse(lastId.Substring(1), out int n))
        {
            next = n + 1;
        }

        return "P" + next.ToString("000");
    }

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
}