using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS_Assignment.Models;


namespace WMS_Assignment.Controllers;


public class ProductController(DB db) : Controller
{
    public IActionResult Menu()
    {

        
        

        return View();
    }

    public IActionResult FilteringMenu()

    {
        var foodcategories = db.FoodCategories;
        var menuItems = db.MenuItems;

        return View(foodcategories);
    }
}