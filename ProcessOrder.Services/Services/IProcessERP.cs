using ProcessOrder.Services.Models;

namespace ProcessOrder.Services.Services;

public interface IProcessERP
{
    Task<OrderItemModel> GetItemForSupplier(string productEAN, string? supplier);

    Task<bool> UpdateStockForItem(string productEAN, int quantity);

    Task<bool> SendXmlData(HttpClient httpclient, StringContent content, string OrderManagementSystemUrl);
}
