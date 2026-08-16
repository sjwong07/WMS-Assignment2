using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS_Assignment.Models;

namespace WMS_Assignment.Controllers;

[Authorize]
public class OrderController(DB db) : Controller
{
    // GET: /Order/History
    public async Task<IActionResult> History()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleId = User.FindFirstValue(ClaimTypes.Role);

        bool isStaffOrAdmin = roleId == "RS01" || roleId == "RA01";

        var query = db.Orders
            .Include(o => o.Table)
            .Include(o => o.User)
            .AsQueryable();

        if (!isStaffOrAdmin)
        {
            query = query.Where(o => o.UserId == userId);
        }

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }
    // GET: /Order/Maintenance — staff/admin view of all orders
    public async Task<IActionResult> Maintenance()
    {
        var roleId = User.FindFirstValue(ClaimTypes.Role);
        if (roleId != "RS01" && roleId != "RA01")
            return Forbid();

        var orders = await db.Orders
            .Include(o => o.User)
            .Include(o => o.Table)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }

    // POST: /Order/UpdateStatus — staff/admin changes an order's status
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(string orderId, string status)
    {
        var roleId = User.FindFirstValue(ClaimTypes.Role);
        if (roleId != "RS01" && roleId != "RA01")
            return Forbid();

        var order = await db.Orders.FindAsync(orderId);
        if (order != null)
        {
            order.Status = status;
            await db.SaveChangesAsync();
        }

        return RedirectToAction("Maintenance");
    }
    // GET: /Order/Report — pie chart of revenue by food category
    public async Task<IActionResult> Report()
    {
        var roleId = User.FindFirstValue(ClaimTypes.Role);
        if (roleId != "RS01" && roleId != "RA01")
            return Forbid();

        var data = await db.OrderDetails
            .Include(od => od.MenuItem).ThenInclude(m => m.Category)
            .GroupBy(od => od.MenuItem.Category.Name)
            .Select(g => new
            {
                Category = g.Key,
                Total = g.Sum(od => od.SubTotal)
            })
            .ToListAsync();

        return View("Piechart", data);
    }


}