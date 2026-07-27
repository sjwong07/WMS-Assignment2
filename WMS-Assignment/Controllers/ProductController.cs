using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS_Assignment.Models;


namespace WMS_Assignment.Controllers;


public class ProductController(DB db) : Controller
{
    public IActionResult Menu(string? search,List<string>? category,decimal?minPrice,decimal? maxPrice) 
    {
        var categories = db.FoodCategories;


        var vm = new MenuItemVM
        {
            Search = search,
            SelectCategories = category,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            FoodCategories = categories,
            MenuItems = db.MenuItems
        };
         
       
        
        return View(vm);
    }

    public IActionResult FilteringMenu()

    {
        var foodcategories = db.FoodCategories;
        var menuItems = db.MenuItems;

        if (Request.IsAjax())
        {
            return PartialView("_Ajax1", menuItems);
        }

        return View(foodcategories);
    }
}