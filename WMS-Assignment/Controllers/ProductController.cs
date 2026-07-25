using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS_Assignment.Models;

namespace WMS_Assignment.Controllers;


public class ProductController(DB db) : Controller
{
    public async Task<IActionResult> Menu()
    {
        var Category = await db.MenuItems.Include(m => m.Category).ToListAsync();
       
        
        return View(Category);
    }
}