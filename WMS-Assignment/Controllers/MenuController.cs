using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WMS_Assignment.Data;
using WMS_Assignment.Models.Entities;
using WMS_Assignment.Models.ViewModels.Menu;

namespace WMS_Assignment.Controllers
{
    [Authorize]
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MenuController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchTerm, int? categoryId)
        {
            var query = _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsAvailable);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(m => m.Name.Contains(searchTerm) ||
                                         m.Description.Contains(searchTerm));
            }

            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(m => m.CategoryId == categoryId.Value);
            }

            var items = await query.OrderBy(m => m.Category.DisplayOrder)
                                   .ThenBy(m => m.DisplayOrder)
                                   .ToListAsync();

            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = categoryId;

            return View(items);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.MenuItemId == id);

            if (item == null)
                return NotFound();

            return View(item);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage()
        {
            var items = await _context.MenuItems
                .Include(m => m.Category)
                .OrderBy(m => m.Category.DisplayOrder)
                .ThenBy(m => m.DisplayOrder)
                .ToListAsync();

            return View(items);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var viewModel = new MenuItemViewModel
            {
                Categories = new SelectList(await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync(), "CategoryId", "CategoryName")
            };
            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuItemViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var menuItem = new MenuItem
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    Price = viewModel.Price,
                    CategoryId = viewModel.CategoryId,
                    IsAvailable = viewModel.IsAvailable,
                    IsVegetarian = viewModel.IsVegetarian,
                    IsSpicy = viewModel.IsSpicy,
                    PreparationTime = viewModel.PreparationTime,
                    DisplayOrder = viewModel.DisplayOrder
                };

                _context.MenuItems.Add(menuItem);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Menu item created successfully!";
                return RedirectToAction(nameof(Manage));
            }

            viewModel.Categories = new SelectList(await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync(), "CategoryId", "CategoryName", viewModel.CategoryId);
            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null)
                return NotFound();

            var viewModel = new MenuItemViewModel
            {
                MenuItemId = item.MenuItemId,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                CategoryId = item.CategoryId,
                IsAvailable = item.IsAvailable,
                IsVegetarian = item.IsVegetarian,
                IsSpicy = item.IsSpicy,
                PreparationTime = item.PreparationTime,
                DisplayOrder = item.DisplayOrder,
                Categories = new SelectList(await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync(), "CategoryId", "CategoryName", item.CategoryId)
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MenuItemViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var item = await _context.MenuItems.FindAsync(viewModel.MenuItemId);
                if (item == null)
                    return NotFound();

                item.Name = viewModel.Name;
                item.Description = viewModel.Description;
                item.Price = viewModel.Price;
                item.CategoryId = viewModel.CategoryId;
                item.IsAvailable = viewModel.IsAvailable;
                item.IsVegetarian = viewModel.IsVegetarian;
                item.IsSpicy = viewModel.IsSpicy;
                item.PreparationTime = viewModel.PreparationTime;
                item.DisplayOrder = viewModel.DisplayOrder;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Menu item updated successfully!";
                return RedirectToAction(nameof(Manage));
            }

            viewModel.Categories = new SelectList(await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync(), "CategoryId", "CategoryName", viewModel.CategoryId);
            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item != null)
            {
                // Check if item is used in any order
                var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.MenuItemId == id);
                if (hasOrders)
                {
                    // Soft delete - just mark as unavailable
                    item.IsAvailable = false;
                    TempData["Warning"] = "Menu item marked as unavailable as it has existing orders.";
                }
                else
                {
                    _context.MenuItems.Remove(item);
                    TempData["Success"] = "Menu item deleted successfully!";
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Manage));
        }
    }
}