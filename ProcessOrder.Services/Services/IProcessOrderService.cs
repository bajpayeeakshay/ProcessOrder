using ProcessOrder.Services.Models;

namespace ProcessOrder.Services.Services;

public interface IProcessOrderService
{
    bool IsFileValid(string[] data);

    Task<OrderModel> GetOrderFromDataAsync(string[] data);

    Task<Tuple<bool, OrderItemModel>> ValidateOrderItem(OrderItemModel orderItem, string? supplierEAN);

    string CreateXmlForOrderData(OrderModel order);
}
