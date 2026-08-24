using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS_Assignment.Models;


namespace WMS_Assignment.Controllers;


public class ProductController(DB db,Helper hp) : Controller
{
   
    public IActionResult Menu(string? search,List<string>? category,decimal?minPrice,decimal? maxPrice) 
    {
        var categories = db.FoodCategories;
        var m = db.MenuItems.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            m = m.Where(m => m.Name.Contains(search));
        }

        if (minPrice.HasValue)
        {
            m = m.Where(p => p.Price >= minPrice);
        }

        if (maxPrice.HasValue)
        {
            m = m.Where(p => p.Price <= maxPrice);
        }

        if(category != null && category.Any())
        {
            m = m.Where(m => category.Contains(m.CategoryId));
        }

        var vm = new MenuItemVM
        {
            Search = search,
            SelectCategories = category,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            FoodCategories = categories,
            MenuItems = m.ToList(),
        };

        if (Request.IsAjax())
        {
            return PartialView("_A", m);
        }


        return View(vm);
    }
    public IActionResult AdminMenu()
    {
        var m = db.MenuItems;
        
        
        return View(m);
    }

   
    public IActionResult Create()
    {
        return View();
    }


    [HttpPost]
    public IActionResult Create(ProductInsertVM vm)
    {
        return View();
    }
    
}