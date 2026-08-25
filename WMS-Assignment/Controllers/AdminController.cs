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
}