using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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

        // Retrieve Member details to get PhotoURL
        var member = await db.Members.FirstOrDefaultAsync(m => m.Username == user.Username);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username ?? ""),
            new Claim(ClaimTypes.NameIdentifier, user.Id ?? ""),
            new Claim(ClaimTypes.Role, user.RoleId ?? "Member"),
            new Claim("PhotoURL", member?.PhotoURL ?? "") // Stores photo filename in claims
        };

        var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
        await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity));

        // Set session variables for layout display
        HttpContext.Session.SetString("User", user.Username ?? "");
        HttpContext.Session.SetString("Role", user.RoleId ?? "Member");
        if (!string.IsNullOrEmpty(member?.PhotoURL))
        {
            HttpContext.Session.SetString("UserPhoto", member.PhotoURL);
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
                PaymentMethod = "Pending", // Fix: NOT NULL database constraint resolved
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
        var userId = GetCurrentUserId();
        if (userId == null) return RedirectToAction("Login");

        var order = await db.Orders
            .Include(o => o.OrderDetails).ThenInclude(od => od.MenuItem)
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
        if (claimUserId != null) return claimUserId;

        // Fall back to session-based login
        var username = HttpContext.Session.GetString("User");
        if (username == null) return null;

        var user = db.Users.FirstOrDefault(u => u.Username == username);
        return user?.Id;
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
                    col.Item().Text($"Date: {order.OrderDate:dd MMM yyyy, hh:mm tt}");
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

                        foreach (var od in order.OrderDetails)
                        {
                            table.Cell().Text(od.MenuItem?.Name ?? "");
                            table.Cell().Text(od.Quantity.ToString());
                            table.Cell().Text($"RM {od.UnitPrice:0.00}");
                            table.Cell().Text($"RM {od.SubTotal:0.00}");
                        }
                    });

                    col.Item().PaddingTop(15).AlignRight().Text($"Total Paid: RM {order.TotalAmount:0.00}").Bold().FontSize(13);
                    col.Item().Text($"Payment Method: {order.PaymentMethod}");
                    col.Item().Text($"Status: {order.PaymentStatus}");
                });

                page.Footer().AlignCenter().Text("Thank you for dining with WagEat Cafe!").FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();

        return File(pdfBytes, "application/pdf", $"Receipt_{order.Id}.pdf");
    }
}