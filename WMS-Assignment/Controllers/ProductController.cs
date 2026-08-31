using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using WMS_Assignment.Models;

namespace WMS_Assignment.Controllers;

public class ProductController(DB db, Helper hp) : Controller
{
    public IActionResult Menu(string? search, List<string>? category, decimal? minPrice, decimal? maxPrice)
    {
        var categories = db.FoodCategories.ToList();

        // Include Category to ensure Category.Name and Category.Id are loaded
        // We added .Include(x => x.MenuItemPhotos) here!
        var m = db.MenuItems
                  .Include(x => x.Category)
                  .Include(x => x.Photos)
                  .AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            m = m.Where(item => item.Name.Contains(search) || item.Id.Contains(search));
        }

        if (minPrice.HasValue)
        {
            m = m.Where(p => p.Price >= minPrice);
        }

        if (maxPrice.HasValue)
        {
            m = m.Where(p => p.Price <= maxPrice);
        }

        if (category != null && category.Any())
        {
            m = m.Where(item => category.Contains(item.CategoryId));
        }

        var menuList = m.ToList();

        var vm = new MenuItemVM
        {
            Search = search,
            SelectCategories = category,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            FoodCategories = categories,
            MenuItems = menuList,
        };

        if (Request.IsAjax())
        {
            return PartialView("_A", vm);
        }

        return View(vm);
    }

    public IActionResult Detail(string id)
    {
        var item = db.MenuItems
            .Include(x => x.Category)
            .Include(x => x.Photos)
            .FirstOrDefault(x => x.Id == id);

        if (item == null) return NotFound();

        return View(item);
    }

}