using Microsoft.EntityFrameworkCore;
using WMS_Assignment.Data;
using WMS_Assignment.Models.Entities;
using WMS_Assignment.Models.ViewModels.Order;

namespace WMS_Assignment.Services
{
    public class OrderService : IOrderService
    {
        private readonly DB _context;

        public OrderService(DB context)
        {
            _context = context;
        }

        public async Task<OrderViewModel> CreateOrderAsync(OrderViewModel model)
        {
            // Generate order number
            var orderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6)}";

            var order = new Order
            {
                OrderNumber = orderNumber,
                UserId = model.UserId,
                TableId = model.TableId,
                SpecialInstructions = model.SpecialInstructions,
                OrderStatus = "Pending",
                PaymentStatus = "Pending"
            };

            // Calculate totals
            decimal subtotal = 0;
            foreach (var item in model.OrderItems)
            {
                var menuItem = await _context.MenuItems.FindAsync(item.MenuItemId);
                if (menuItem != null)
                {
                    var orderItem = new OrderItem
                    {
                        MenuItemId = item.MenuItemId,
                        Quantity = item.Quantity,
                        UnitPrice = menuItem.Price,
                        Subtotal = menuItem.Price * item.Quantity,
                        SpecialInstructions = item.SpecialInstructions
                    };
                    order.OrderItems.Add(orderItem);
                    subtotal += orderItem.Subtotal;
                }
            }

            order.Subtotal = subtotal;
            order.Tax = subtotal * 0.06m; // 6% tax
            order.ServiceCharge = subtotal * 0.10m; // 10% service charge
            order.TotalAmount = subtotal + order.Tax + order.ServiceCharge;

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            // Mark table as occupied
            if (order.TableId.HasValue)
            {
                var table = await _context.Tables.FindAsync(order.TableId.Value);
                if (table != null)
                {
                    table.IsOccupied = true;
                    await _context.SaveChangesAsync();
                }
            }

            return await GetOrderByIdAsync(order.OrderId);
        }

        public async Task<OrderViewModel> GetOrderByIdAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(m => m.Category)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return null;

            return MapToViewModel(order);
        }

        public async Task<IEnumerable<OrderViewModel>> GetAllOrdersAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(MapToViewModel);
        }

        public async Task<IEnumerable<OrderViewModel>> GetOrdersByUserAsync(int userId)
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(MapToViewModel);
        }

        public async Task<IEnumerable<OrderViewModel>> GetOrdersByStatusAsync(string status)
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Where(o => o.OrderStatus == status)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(MapToViewModel);
        }

        public async Task<OrderViewModel> UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return null;

            order.OrderStatus = status;

            if (status == "Completed")
            {
                order.CompletedDate = DateTime.Now;
                // Release table
                if (order.TableId.HasValue)
                {
                    var table = await _context.Tables.FindAsync(order.TableId.Value);
                    if (table != null)
                    {
                        table.IsOccupied = false;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return await GetOrderByIdAsync(orderId);
        }

        public async Task<OrderViewModel> UpdateOrderAsync(OrderViewModel model)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == model.OrderId);

            if (order == null)
                return null;

            // Update order details
            order.SpecialInstructions = model.SpecialInstructions;

            // Update order items (simplified - in production you'd handle add/remove/edit)
            // For simplicity, we'll remove existing items and add new ones
            _context.OrderItems.RemoveRange(order.OrderItems);

            decimal subtotal = 0;
            foreach (var item in model.OrderItems)
            {
                var menuItem = await _context.MenuItems.FindAsync(item.MenuItemId);
                if (menuItem != null)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        MenuItemId = item.MenuItemId,
                        Quantity = item.Quantity,
                        UnitPrice = menuItem.Price,
                        Subtotal = menuItem.Price * item.Quantity,
                        SpecialInstructions = item.SpecialInstructions
                    };
                    _context.OrderItems.Add(orderItem);
                    subtotal += orderItem.Subtotal;
                }
            }

            order.Subtotal = subtotal;
            order.Tax = subtotal * 0.06m;
            order.ServiceCharge = subtotal * 0.10m;
            order.TotalAmount = subtotal + order.Tax + order.ServiceCharge;

            await _context.SaveChangesAsync();
            return await GetOrderByIdAsync(order.OrderId);
        }

        public async Task<bool> CancelOrderAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return false;

            if (order.OrderStatus == "Completed" || order.OrderStatus == "Cancelled")
                return false;

            order.OrderStatus = "Cancelled";

            // Release table
            if (order.TableId.HasValue)
            {
                var table = await _context.Tables.FindAsync(order.TableId.Value);
                if (table != null)
                {
                    table.IsOccupied = false;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> CalculateOrderTotalAsync(List<OrderItemViewModel> items)
        {
            decimal subtotal = 0;
            foreach (var item in items)
            {
                var menuItem = await _context.MenuItems.FindAsync(item.MenuItemId);
                if (menuItem != null)
                {
                    subtotal += menuItem.Price * item.Quantity;
                }
            }
            var tax = subtotal * 0.06m;
            var serviceCharge = subtotal * 0.10m;
            return subtotal + tax + serviceCharge;
        }

        public async Task<bool> IsTableAvailableAsync(int tableId)
        {
            var table = await _context.Tables.FindAsync(tableId);
            if (table == null)
                return false;
            return !table.IsOccupied;
        }

        private OrderViewModel MapToViewModel(Order order)
        {
            return new OrderViewModel
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                UserId = order.UserId,
                UserName = $"{order.User?.FirstName} {order.User?.LastName}",
                TableId = order.TableId,
                TableNumber = order.Table?.TableNumber,
                OrderDate = order.OrderDate,
                Subtotal = order.Subtotal,
                Tax = order.Tax,
                ServiceCharge = order.ServiceCharge,
                TotalAmount = order.TotalAmount,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                SpecialInstructions = order.SpecialInstructions,
                OrderItems = order.OrderItems.Select(oi => new OrderItemViewModel
                {
                    MenuItemId = oi.MenuItemId,
                    MenuItemName = oi.MenuItem.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Subtotal = oi.Subtotal,
                    SpecialInstructions = oi.SpecialInstructions
                }).ToList()
            };
        }
    }
}