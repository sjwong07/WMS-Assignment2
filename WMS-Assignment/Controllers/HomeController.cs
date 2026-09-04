using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using WMS_Assignment.Models;

namespace WMS_Assignment.Controllers;

public class HomeController(DB db, Helper hp) : Controller
{
    public async Task<IActionResult> Index()
    {
        // Fetch 2 Main Dishes, 2 Side Dishes (snacks), and 2 Drinks/Desserts
        var mainDishes = await db.MenuItems
            .Include(m => m.Category)
            .Where(m => m.Category != null && m.Category.Name.Contains("Main"))
            .Take(2)
            .ToListAsync();

        var sideDishes = await db.MenuItems
            .Include(m => m.Category)
            .Where(m => m.Category != null && m.Category.Name.Contains("Side"))
            .Take(2)
            .ToListAsync();

        var drinks = await db.MenuItems
            .Include(m => m.Category)
            .Where(m => m.Category != null && (m.Category.Name.Contains("Drink") || m.Category.Name.Contains("Dessert")))
            .Take(2)
            .ToListAsync();

        // Combine them into a single list of 6 items
        var featuredItems = mainDishes.Concat(sideDishes).Concat(drinks).ToList();

        // Fallback if categories don't match exact strings: take any 6 if list is short
        if (featuredItems.Count < 6)
        {
            featuredItems = await db.MenuItems
                .Include(m => m.Category)
                .Take(6)
                .ToListAsync();
        }

        return View(featuredItems);
    }

    //---------------- Menu Routing Fix ----------------
    public IActionResult Menu()
    {
        // Redirects to your Product Menu catalog
        return RedirectToAction("Menu", "Product");
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    // GET: /Home/Reviews
    public async Task<IActionResult> Reviews()
    {
        var reviews = await db.Reviews
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        ViewBag.Reviews = reviews;
        return View();
    }

    // POST: /Home/PostReview
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostReview(int rating, string? comment)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            TempData["Error"] = "You must be logged in to post a review. (userId was null)";
            return RedirectToAction("Reviews");
        }

        if (rating < 1 || rating > 5)
        {
            TempData["Error"] = "Please select a rating.";
            return RedirectToAction("Reviews");
        }

        var review = new Review
        {
            UserId = userId,
            Rating = rating,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            CreatedAt = DateTime.Now
        };

        db.Reviews.Add(review);
        int rowsSaved = await db.SaveChangesAsync();

        TempData["Success"] = $"Review saved! Rows affected: {rowsSaved}, New Review Id: {review.Id}";

        return RedirectToAction("Reviews");
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

    public IActionResult Login()
    {
        return View("~/Views/Security/Login.cshtml");
    }



    [HttpPost]
    public async Task<IActionResult> Login(string Username, string Password)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == Username);

        if (user == null)
        {
            ViewBag.Message = "Invalid Username or Password.";
            return View("~/Views/Security/Login.cshtml");
        }

        // Check if account is currently locked out
        if (user.LockoutEnd != null && user.LockoutEnd > DateTime.Now)
        {
            var minutesLeft = Math.Ceiling((user.LockoutEnd.Value - DateTime.Now).TotalMinutes);
            ViewBag.Message = $"Account locked. Try again in {minutesLeft} minute(s).";
            return View("~/Views/Security/Login.cshtml");
        }

        // Verify using the password hash
        bool isPasswordCorrect = !string.IsNullOrEmpty(user.Hash) && hp.VerifyPassword(user.Hash, Password);

        if (!isPasswordCorrect)
        {
            user.FailedLogin += 1;

            if (user.FailedLogin >= 3)
            {
                user.LockoutEnd = DateTime.Now.AddMinutes(5);
                user.FailedLogin = 0;
                await db.SaveChangesAsync();
                ViewBag.Message = "Too many failed attempts. Account locked for 5 minutes.";
                return View("~/Views/Security/Login.cshtml");
            }

            await db.SaveChangesAsync();
            ViewBag.Message = $"Invalid Username or Password. {3 - user.FailedLogin} attempt(s) remaining.";
            return View("~/Views/Security/Login.cshtml");
        }

        // Successful login — reset failed attempts
        user.FailedLogin = 0;
        user.LockoutEnd = null;
        await db.SaveChangesAsync();

        // Retrieve user record to capture PhotoURL for both members and admins
        var userRecord = await db.Users.FirstOrDefaultAsync(u => u.Username == user.Username);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username ?? ""),
            new Claim(ClaimTypes.NameIdentifier, user.Id ?? ""),
            new Claim(ClaimTypes.Role, user.RoleId ?? "Member"),
            new Claim("PhotoURL", userRecord?.PhotoURL ?? "")
        };

        var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
        await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity));

        // Set session variables for layout display
        HttpContext.Session.SetString("User", user.Username ?? "");
        HttpContext.Session.SetString("Role", user.RoleId ?? "Member");
        var activePhoto = userRecord?.PhotoURL;
        if (!string.IsNullOrEmpty(activePhoto))
        {
            HttpContext.Session.SetString("UserPhoto", activePhoto);
        }

        return RedirectToAction("Menu", "Product");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("CookieAuth");
        HttpContext.Session.Clear();
        return RedirectToAction("Login", "Security");
    }

    public async Task<IActionResult> Cart()
    {
        var userId = GetCurrentUserId();
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
        var userId = GetCurrentUserId();
        if (userId == null) return RedirectToAction("Login");

        // Verify that the user actually exists in the db.Users table to prevent foreign key exception
        var userExists = await db.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            return RedirectToAction("Login");
        }

        if (string.IsNullOrEmpty(menuItemId) || quantity < 1)
            return RedirectToAction("Menu", "Product");

        var menuItem = await db.MenuItems.FindAsync(menuItemId);
        if (menuItem == null) return RedirectToAction("Menu", "Product");

        // Ensure a valid table exists to avoid foreign key errors
        if (string.IsNullOrEmpty(tableId))
        {
            var firstTable = await db.Tables.Select(t => t.Id).FirstOrDefaultAsync();
            tableId = firstTable ?? "T01";
        }

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
                PaymentMethod = "Pending",
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
    public async Task<IActionResult> UpdateTable(string orderId, string tableId)
    {
        var order = await db.Orders.FindAsync(orderId);
        if (order != null && order.Status == "Pending")
        {
            order.TableId = tableId;
            await db.SaveChangesAsync();
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
        var userId = GetCurrentUserId();
        if (userId == null) return RedirectToAction("Login");

        var order = await db.Orders
            .Include(o => o.OrderDetails).ThenInclude(od => od.MenuItem)
            .Include(o => o.Table)
            .Where(o => o.UserId == userId && o.Status == "Pending")
            .FirstOrDefaultAsync();

        if (order == null || order.OrderDetails == null || !order.OrderDetails.Any())
            return RedirectToAction("Cart");

        // Force table selection validation before allowing entry to checkout/payment
        if (string.IsNullOrEmpty(order.TableId))
        {
            TempData["Error"] = "Please select your table number before proceeding to checkout.";
            return RedirectToAction("Cart");
        }

        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(string orderId, string paymentMethod)
    {
        var order = await db.Orders.FindAsync(orderId);
        if (order == null) return RedirectToAction("Cart");

        if (string.IsNullOrEmpty(order.TableId))
        {
            TempData["Error"] = "Please select your table number before proceeding to checkout.";
            return RedirectToAction("Cart");
        }

        order.PaymentMethod = string.IsNullOrEmpty(paymentMethod) ? "Cash" : paymentMethod;
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

    [HttpGet]
    public async Task<IActionResult> FilterMenu(string search, string category, decimal? minPrice, decimal? maxPrice, string sortOrder, int page = 1, int pageSize = 8)
    {
        var query = db.MenuItems.Include(m => m.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Id.Contains(search) || p.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            query = query.Where(p => (p.Category != null && p.Category.Name == category) || p.CategoryId == category);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        query = sortOrder switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name_desc" => query.OrderByDescending(p => p.Name),
            _ => query.OrderBy(p => p.Name),
        };

        int totalItems = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var model = new
        {
            MenuItems = items,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };

        return PartialView("_A", model);
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

    private string? GetCurrentUserId()
    {
        // Try real cookie/claims login first
        var claimUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(claimUserId))
        {
            var userById = db.Users.FirstOrDefault(u => u.Id == claimUserId);
            if (userById != null) return userById.Id;
        }

        // Fall back to matching by username from claims or session
        var username = User.Identity?.Name ?? HttpContext.Session.GetString("User");
        if (!string.IsNullOrEmpty(username))
        {
            var user = db.Users.FirstOrDefault(u => u.Username!.ToLower() == username.ToLower());
            if (user != null) return user.Id;
        }

        return null;
    }

    // GET: /Home/ReceiptPdf/{id}
    public async Task<IActionResult> ReceiptPdf(string id)
    {
        var order = await db.Orders
            .Include(o => o.OrderDetails).ThenInclude(od => od.MenuItem)
            .Include(o => o.Table)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A5);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("WagEat Cafe").FontSize(20).Bold();
                    col.Item().Text("E-Receipt").FontSize(14).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Item().Text($"Order ID: {order.Id}");
                    col.Item().Text($"Customer: {order.User?.Name}");
                    col.Item().Text($"Table: {order.Table?.Id}");
                    col.Item().Text($"Date: {order.OrderDate.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture)}");
                    col.Item().PaddingTop(10);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Item").Bold();
                            header.Cell().Text("Qty").Bold();
                            header.Cell().Text("Unit Price").Bold();
                            header.Cell().Text("Subtotal").Bold();
                        });

                        foreach (var od in order.OrderDetails!)
                        {
                            table.Cell().Text(od.MenuItem?.Name ?? "");
                            table.Cell().Text(od.Quantity.ToString(CultureInfo.InvariantCulture));
                            table.Cell().Text(string.Format(CultureInfo.InvariantCulture, "RM {0:0.00}", od.UnitPrice));
                            table.Cell().Text(string.Format(CultureInfo.InvariantCulture, "RM {0:0.00}", od.SubTotal));
                        }
                    });

                    col.Item().PaddingTop(15).AlignRight().Text(string.Format(CultureInfo.InvariantCulture, "Total Paid: RM {0:0.00}", order.TotalAmount)).Bold().FontSize(13);
                    col.Item().Text($"Payment Method: {order.PaymentMethod}");
                    col.Item().Text($"Status: {order.PaymentStatus}");
                });

                page.Footer().AlignCenter().Text("Thank you for dining with WagEat Cafe!").FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();

        return File(pdfBytes, "application/pdf", $"Receipt_{order.Id}.pdf");
    }
}