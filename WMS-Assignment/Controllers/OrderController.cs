using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS_Assignment.Models;

namespace WMS_Assignment.Controllers;

public class OrderController(DB db) : Controller
{
    private string GetCurrentUserId()
    {
        var username = HttpContext.Session.GetString("User");
        if (username == null) return null;
        var user = db.Users.FirstOrDefault(u => u.Username == username);
        return user?.Id;
    }

    private string GetCurrentRoleId()
    {
        var username = HttpContext.Session.GetString("User");
        if (username == null) return null;
        var user = db.Users.FirstOrDefault(u => u.Username == username);
        return user?.RoleId;
    }

    // GET: /Order/History
    public async Task<IActionResult> History()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return RedirectToAction("Login", "Security");

        var roleId = GetCurrentRoleId();
        bool isStaffOrAdmin = IsStaffOrAdmin(roleId);

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

    // GET: /Order/Maintenance
    public async Task<IActionResult> Maintenance()
    {
        var roleId = GetCurrentRoleId();
        if (roleId == null) return RedirectToAction("Login", "Security");
        if (!IsStaffOrAdmin(roleId))
            return Forbid();

        var orders = await db.Orders
            .Include(o => o.User)
            .Include(o => o.Table)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }

    // POST: /Order/UpdateStatus
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(string orderId, string status)
    {
        var roleId = GetCurrentRoleId();
        if (roleId == null) return RedirectToAction("Login", "Security");
        if (!IsStaffOrAdmin(roleId))
            return Forbid();

        var order = await db.Orders.FindAsync(orderId);
        if (order != null)
        {
            order.Status = status;
            await db.SaveChangesAsync();
        }

        return RedirectToAction("Maintenance");
    }

    // GET: /Order/Report
    public async Task<IActionResult> Report()
    {
        var roleId = GetCurrentRoleId();
        if (roleId == null) return RedirectToAction("Login", "Security");
        if (!IsStaffOrAdmin(roleId))
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

    private bool IsStaffOrAdmin(string roleId)
    {
        return roleId == "RS01" || roleId == "RA01" || roleId == "Admin";
    }
}