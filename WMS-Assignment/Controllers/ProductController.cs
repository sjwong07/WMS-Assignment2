using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS_Assignment.Models;


namespace WMS_Assignment.Controllers;


public class ProductController(DB db) : Controller
{
    public IActionResult Menu(string? search,List<string>? category,decimal?minPrice,decimal? maxPrice,string? name) 
    {
        var categories = db.FoodCategories;
        var m = db.MenuItems.AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {
            m = m.Where(m => m.Name.Contains(name));
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

        if (minPrice != null)
        {
            m = m.Where(p => p.Price >= minPrice);
        }

        if (maxPrice != null)
        {
            m = m.Where(p => p.Price >= maxPrice);
        }



        if (Request.IsAjax())
        {
            return PartialView("_A", m);
        }


        return View(vm);
    }

    


    public IActionResult FilteringMenu(string? search, List<string>? category, decimal? minPrice,decimal? maxPrice)

    {
        var foodcategories = db.FoodCategories;
        var m = db.MenuItems.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            m = m.Where(p => p.Name.Contains(search));


        }


        

        return View(m);
    }

    
}