using WMS_Assignment.Models.ViewModels.Order;

namespace WMS_Assignment.Services
{
    public interface IOrderService
    {
        Task<OrderViewModel> CreateOrderAsync(OrderViewModel model);
        Task<OrderViewModel> GetOrderByIdAsync(int orderId);
        Task<IEnumerable<OrderViewModel>> GetAllOrdersAsync();
        Task<IEnumerable<OrderViewModel>> GetOrdersByUserAsync(int userId);
        Task<IEnumerable<OrderViewModel>> GetOrdersByStatusAsync(string status);
        Task<OrderViewModel> UpdateOrderStatusAsync(int orderId, string status);
        Task<OrderViewModel> UpdateOrderAsync(OrderViewModel model);
        Task<bool> CancelOrderAsync(int orderId);
        Task<decimal> CalculateOrderTotalAsync(List<OrderItemViewModel> items);
        Task<bool> IsTableAvailableAsync(int tableId);
    }
}