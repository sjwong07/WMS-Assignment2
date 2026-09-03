using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using WMS_Assignment.Models;

namespace WMS_Assignment.Controllers;

public class ProductController(DB db, Helper hp) : Controller
{
    private string? GetCurrentUserId()
    {
        var username = HttpContext.Session.GetString("User") ?? User.Identity?.Name;
        if (username == null) return null;
        var user = db.Users.FirstOrDefault(u => u.Username != null && u.Username.ToLower() == username.ToLower());
        return user?.Id;
    }

    public IActionResult Menu(string? search, List<string>? category, decimal? minPrice, decimal? maxPrice)
    {
        var categories = db.FoodCategories.ToList();

        var m = db.MenuItems
                  .Include(x => x.Category)
                  .Include(x => x.Photos)
                  .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            m = m.Where(item => item.Name.Contains(search) || item.Id.Contains(search));
        }

        if (minPrice.HasValue)
        {
            m = m.Where(p => p.Price >= minPrice);
        }

        if (maxPrice.HasValue)
        {
            m = m.Where(p => p.Price <= maxPrice);
        }

        if (category != null && category.Any())
        {
            m = m.Where(item => category.Contains(item.CategoryId));
        }

        var menuList = m.ToList();

        var vm = new MenuItemVM
        {
            Search = search,
            SelectCategories = category,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            FoodCategories = categories,
            MenuItems = menuList,
        };

        if (Request.IsAjax())
        {
            return PartialView("_A", vm);
        }

        return View(vm);
    }

    public IActionResult Detail(string id)
    {
        var item = db.MenuItems
            .Include(x => x.Category)
            .Include(x => x.Photos)
            .Include(x => x.Reviews)
                .ThenInclude(r => r.User)
            .FirstOrDefault(x => x.Id == id);

        if (item == null) return NotFound();

        return View(item);
    }

    // POST: /Product/SubmitReview
    [HttpPost]
    [Authorize(Roles = "Member")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview(string menuItemId, int rating, string comment)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return RedirectToAction("Login", "Security");

        // Verify that the user has a completed/paid order containing this menu item
        bool hasPurchased = await db.Orders
            .Where(o => o.UserId == userId && o.PaymentStatus == "Paid")
            .SelectMany(o => o.OrderDetails)
            .AnyAsync(od => od.MenuItemId == menuItemId);

        if (!hasPurchased)
        {
            TempData["Error"] = "You can only review items that you have previously purchased.";
            return RedirectToAction("Detail", new { id = menuItemId });
        }

        var review = new WMS_Assignment.Models.Review
        {
            MenuItemId = menuItemId,
            UserId = userId,
            Rating = Math.Clamp(rating, 1, 5),
            Comment = comment?.Trim(),
            CreatedAt = DateTime.Now
        };

        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        TempData["Success"] = "Review submitted successfully!";
        return RedirectToAction("Detail", new { id = menuItemId });
    }
}