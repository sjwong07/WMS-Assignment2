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
        var items = await db.MenuItems.Include(m => m.Category).ToListAsync();
        var tables = await db.Tables.ToListAsync();
        ViewBag.Tables = tables;
        return View(items);
    }
}