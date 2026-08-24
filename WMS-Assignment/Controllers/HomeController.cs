using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using WMS_Assignment.Models;

namespace WMS_Assignment.Controllers;

public class HomeController(DB db, Helper hp) : Controller
{
    public async Task<IActionResult> Index()
    {
        // Fetch featured items
        var featuredItems = await db.MenuItems
            .Include(m => m.Category)
            .Take(4)
            .ToListAsync();

        return View(featuredItems);
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    public async Task<IActionResult> Receipt(string id)
    {
        var order = await db.Orders
            .Include(o => o.OrderDetails!).ThenInclude(od => od.MenuItem)
            .Include(o => o.Table)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        return View(order);
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("CookieAuth");
        HttpContext.Session.Clear();
        return RedirectToAction("Login", "Security");
    }

    public async Task<IActionResult> Cart()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return RedirectToAction("Login");

        var order = await db.Orders
            .Include(o => o.OrderDetails!).ThenInclude(od => od.MenuItem)
            .Include(o => o.Table)
            .Where(o => o.UserId == userId && o.Status == "Pending")
            .OrderByDescending(o => o.OrderDate)
            .FirstOrDefaultAsync();

        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> AddToOrder(string tableId, string menuItemId, int quantity)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return RedirectToAction("Login");

        if (string.IsNullOrEmpty(menuItemId) || quantity < 1)
            return RedirectToAction("Menu", "Product");

        var menuItem = await db.MenuItems.FindAsync(menuItemId);
        if (menuItem == null) return RedirectToAction("Menu", "Product");

        var order = await db.Orders
            .Include(o => o.OrderDetails)
            .Where(o => o.UserId == userId && o.Status == "Pending")
            .FirstOrDefaultAsync();

        if (order == null)
        {
            order = new Order
            {
                Id = "O" + Guid.NewGuid().ToString("N")[..8],
                UserId = userId,
                TableId = tableId,
                OrderDate = DateTime.Now,
                Status = "Pending",
                PaymentStatus = "Unpaid",
                TotalAmount = 0,
                OrderDetails = new List<OrderDetail>()
            };
            db.Orders.Add(order);
        }

        var existingDetail = order.OrderDetails?.FirstOrDefault(od => od.MenuItemId == menuItemId);
        if (existingDetail != null)
        {
            existingDetail.Quantity += quantity;
            existingDetail.SubTotal = existingDetail.Quantity * existingDetail.UnitPrice;
        }
        else
        {
            db.OrderDetails.Add(new OrderDetail
            {
                Id = "OD" + Guid.NewGuid().ToString("N")[..8],
                OrderId = order.Id,
                MenuItemId = menuItemId,
                Quantity = quantity,
                UnitPrice = menuItem.Price,
                SubTotal = menuItem.Price * quantity
            });
        }

        await db.SaveChangesAsync();
        await RecalculateTotal(order.Id);

        return RedirectToAction("Cart");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(string orderDetailId, int quantity)
    {
        var detail = await db.OrderDetails.FindAsync(orderDetailId);
        if (detail != null && quantity > 0)
        {
            detail.Quantity = quantity;
            detail.SubTotal = detail.Quantity * detail.UnitPrice;
            await db.SaveChangesAsync();
            await RecalculateTotal(detail.OrderId!);
        }
        return RedirectToAction("Cart");
    }

    [HttpPost]
    public async Task<IActionResult> RemoveItem(string orderDetailId)
    {
        var detail = await db.OrderDetails.FindAsync(orderDetailId);
        if (detail != null)
        {
            string? orderId = detail.OrderId;
            db.OrderDetails.Remove(detail);
            await db.SaveChangesAsync();
            if (!string.IsNullOrEmpty(orderId))
            {
                await RecalculateTotal(orderId);
            }
        }
        return RedirectToAction("Cart");
    }

    public async Task<IActionResult> Checkout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return RedirectToAction("Login");

        var order = await db.Orders
            .Include(o => o.OrderDetails)
            .Where(o => o.UserId == userId && o.Status == "Pending")
            .FirstOrDefaultAsync();

        if (order == null || order.OrderDetails == null || !order.OrderDetails.Any())
            return RedirectToAction("Cart");

        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(string orderId, string paymentMethod)
    {
        var order = await db.Orders.FindAsync(orderId);
        if (order == null) return RedirectToAction("Cart");

        order.PaymentMethod = paymentMethod;
        order.PaymentStatus = "Paid";
        order.Status = "Preparing";
        await db.SaveChangesAsync();

        return RedirectToAction("Receipt", new { id = order.Id });
    }

    private async Task RecalculateTotal(string orderId)
    {
        var order = await db.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order != null && order.OrderDetails != null)
        {
            order.TotalAmount = order.OrderDetails.Sum(od => od.SubTotal);
            await db.SaveChangesAsync();
        }
    }

    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
        );

        return LocalRedirect(returnUrl ?? "~/");
    }
}