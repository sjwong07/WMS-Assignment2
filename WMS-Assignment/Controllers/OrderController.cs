using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS_Assignment.Data;
using WMS_Assignment.Models.Entities;
using WMS_Assignment.Models.ViewModels.Order;
using WMS_Assignment.Services;

namespace WMS_Assignment.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly DB _context;
        private readonly IOrderService _orderService;

        public OrderController(DB context, IOrderService orderService)
        {
            _context = context;
            _orderService = orderService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var isAdmin = User.IsInRole("Admin");
            var isStaff = User.IsInRole("Staff");

            List<OrderViewModel> orders;
            if (isAdmin || isStaff)
            {
                orders = (await _orderService.GetAllOrdersAsync()).ToList();
            }
            else
            {
                orders = (await _orderService.GetOrdersByUserAsync(userId)).ToList();
            }

            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound();

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var isAdmin = User.IsInRole("Admin");
            var isStaff = User.IsInRole("Staff");

            if (!isAdmin && !isStaff && order.UserId != userId)
                return Forbid();

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new OrderViewModel
            {
                OrderDate = DateTime.Now,
                Tables = new SelectList(await _context.Tables
                    .Where(t => t.IsActive && !t.IsOccupied)
                    .ToListAsync(), "TableId", "TableNumber")
            };

            ViewBag.MenuItems = await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsAvailable)
                .OrderBy(m => m.Category.DisplayOrder)
                .ThenBy(m => m.DisplayOrder)
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Check table availability
                if (!await _orderService.IsTableAvailableAsync(viewModel.TableId.Value))
                {
                    ModelState.AddModelError("TableId", "Table is not available.");
                    viewModel.Tables = new SelectList(await _context.Tables
                        .Where(t => t.IsActive && !t.IsOccupied)
                        .ToListAsync(), "TableId", "TableNumber");
                    ViewBag.MenuItems = await _context.MenuItems
                        .Include(m => m.Category)
                        .Where(m => m.IsAvailable)
                        .ToListAsync();
                    return View(viewModel);
                }

                viewModel.UserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var order = await _orderService.CreateOrderAsync(viewModel);

                TempData["Success"] = "Order created successfully!";
                return RedirectToAction(nameof(Details), new { id = order.OrderId });
            }

            viewModel.Tables = new SelectList(await _context.Tables
                .Where(t => t.IsActive && !t.IsOccupied)
                .ToListAsync(), "TableId", "TableNumber");
            ViewBag.MenuItems = await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsAvailable)
                .ToListAsync();
            return View(viewModel);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _orderService.UpdateOrderStatusAsync(id, status);
            if (order == null)
                return NotFound();

            TempData["Success"] = $"Order status updated to {status}";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _orderService.CancelOrderAsync(id);
            if (result)
            {
                TempData["Success"] = "Order cancelled successfully.";
            }
            else
            {
                TempData["Error"] = "Unable to cancel order.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var orders = await _orderService.GetOrdersByUserAsync(userId);

            // Filter completed orders
            orders = orders.Where(o => o.OrderStatus == "Completed" || o.OrderStatus == "Cancelled");

            return View(orders);
        }

        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Manage()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return View(orders);
        }

        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> KitchenView()
        {
            var pendingOrders = await _orderService.GetOrdersByStatusAsync("Pending");
            var confirmedOrders = await _orderService.GetOrdersByStatusAsync("Confirmed");
            var preparingOrders = await _orderService.GetOrdersByStatusAsync("Preparing");

            ViewBag.PendingOrders = pendingOrders;
            ViewBag.ConfirmedOrders = confirmedOrders;
            ViewBag.PreparingOrders = preparingOrders;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderItems(int orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
                return Json(new { success = false, message = "Order not found" });

            return Json(new { success = true, data = order });
        }

        [HttpGet]
        public async Task<IActionResult> GetTableStatus()
        {
            var tables = await _context.Tables
                .Where(t => t.IsActive)
                .Select(t => new
                {
                    t.TableId,
                    t.TableNumber,
                    t.Capacity,
                    t.Location,
                    IsOccupied = t.IsOccupied,
                    Status = t.IsOccupied ? "Occupied" : "Available"
                })
                .ToListAsync();

            return Json(new { success = true, data = tables });
        }
    }
}