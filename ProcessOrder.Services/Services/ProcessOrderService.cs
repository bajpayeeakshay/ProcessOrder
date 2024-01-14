using ProcessOrder.Services.Models;
using ProcessOrder.Services.Models.Settings;
using System.Xml.Serialization;
using Serilog;

namespace ProcessOrder.Services.Services;

public class ProcessOrderService : IProcessOrderService
{
    private readonly IProcessERP _processERP;
    private readonly FileFormatSettings _fileFormatSettings;
    private ILogger _logger;
    private readonly IEmailNotifier _emailNotifier;
    private readonly AppSettings _appSettings;

    public ProcessOrderService(ILogger logger, 
        IProcessERP processERP, 
        FileFormatSettings fileFormatSettings,
        IEmailNotifier emailNotifier, 
        AppSettings appSettings)
    {
        _logger = logger;
        _processERP = processERP;
        _fileFormatSettings = fileFormatSettings;
        _emailNotifier = emailNotifier;
        _appSettings = appSettings;
    }

    public bool IsFileValid(string[] data)
    {
        var fileTypeIdentifierLength = _fileFormatSettings.Order.FileTypeIdentifier.Length;

        if(data == null || data.Length <= 0 || data[0].Length <= fileTypeIdentifierLength || data[0].Substring(0, fileTypeIdentifierLength) != "ORD")
        {
            return false;
        }

        _logger.Information("Data received is an ORDER file, validate successfully");
        return true;
    }

    public async Task<OrderModel> GetOrderFromDataAsync(string[] data)
    {
        var orderSettings = _fileFormatSettings.Order;
        var orderItemSetting = _fileFormatSettings.OrderItem;
        OrderModel order = new OrderModel();

        // Parse order header
        order.OrderNumber = data[0].Substring(orderSettings.OrderNumber.Start, orderSettings.OrderNumber.Length).Trim();
        order.OrderDate = data[0].Substring(orderSettings.OrderDate.Start, orderSettings.OrderDate.Length).Trim();
        order.BuyerEAN = data[0].Substring(orderSettings.BuyerEAN.Start, orderSettings.BuyerEAN.Length).Trim();
        order.SupplierEAN = data[0].Substring(orderSettings.SupplierEAN.Start, orderSettings.SupplierEAN.Length).Trim();
        order.Comment = data[0].Substring(orderSettings.Comment.Start, orderSettings.Comment.Length).Trim();

        // Parse order data
        for (int i = 1; i < data.Length; i++)
        {
            OrderItemModel orderItem = new OrderItemModel
            {
                EAN = data[i].Substring(orderItemSetting.EAN.Start, orderItemSetting.EAN.Length).Trim(),
                Description = data[i].Substring(orderItemSetting.Description.Start, orderItemSetting.Description.Length).Trim(),
                Quantity = int.Parse(data[i].Substring(orderItemSetting.Quantity.Start, orderItemSetting.Quantity.Length).Trim()),
                UnitPrice = decimal.Parse(data[i].Substring(orderItemSetting.UnitPrice.Start, orderItemSetting.UnitPrice.Length).Trim())
            };

            // Validate article, check unit price, stock, etc.
            var validValues = await ValidateOrderItem(orderItem, order.SupplierEAN);

            if (validValues.Item1)
            {
                _logger.Information($"Product {orderItem.EAN} Validated Successfully");
                await _processERP.UpdateStockForItem(validValues.Item2.EAN, validValues.Item2.Quantity);
                _logger.Information($"Product {orderItem.EAN} Stock updated successfully in ERP");
                order.OrderItems.Add(validValues.Item2);
            }
            else
            {
                throw new InvalidOperationException($"Error in processing data for {orderItem.EAN}");
            }
        }
        return order;
    }

    public async Task<Tuple<bool, OrderItemModel>> ValidateOrderItem(OrderItemModel orderItem, string? supplierEAN)
    {
        var erpItem = await _processERP.GetItemForSupplier(orderItem.EAN, supplierEAN);

        if(erpItem == null)
        {
            _logger.Error($"Item - {orderItem.EAN} not found in ERP");
            return new Tuple<bool, OrderItemModel>(false, orderItem);
        }

        if(erpItem.UnitPrice != orderItem.UnitPrice)
        {
            orderItem.UnitPrice = erpItem.UnitPrice;
            var message = $"Unit Price for Product {orderItem.EAN} Received from " +
                $"ERP {erpItem.UnitPrice} is lower than in the file {orderItem.UnitPrice} for supplier-{supplierEAN} ";
            _logger.Information(message);
            await _emailNotifier.SendNotificationAsync(_appSettings.AccountManagerEmail,
                    $"Warning | Price Mismatch - {orderItem.EAN}",
                    message);
        }

        if(erpItem.Quantity < orderItem.Quantity)
        {
            var message = $"Product {orderItem.EAN} is out of stock. Request QTY: {orderItem.Quantity}, Qty In Stock: {erpItem.Quantity}";
            await _emailNotifier.SendNotificationAsync(_appSettings.AccountManagerEmail,
                    $"Error | Out Of Stock - {orderItem.EAN}",
                    message);
            _logger.Error(message);
            return new Tuple<bool, OrderItemModel>(false, orderItem);
        }

        return new Tuple<bool, OrderItemModel>(true, orderItem);
    }

    public string CreateXmlForOrderData(OrderModel order)
    {
        var xmlSerializer = new XmlSerializer(order.GetType());
        using (var writer = new StringWriter())
        {
            xmlSerializer.Serialize(writer, order);
            _logger.Information("XML file created successfully");
            return writer.ToString();
        }
    }
}
