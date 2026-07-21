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
        

        // Staff (RS01) and Admin (RA01) see every order.
        // Customer (RC01) sees only their own orders.
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
}