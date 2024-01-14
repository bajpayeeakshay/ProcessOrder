using Serilog;
using ProcessOrder.Services.Models;
using ProcessOrder.Services.Models.Settings;

namespace ProcessOrder.Services.Services;

public class ProcessERP : IProcessERP
{
    private readonly IEmailNotifier _emailNotifier;
    private readonly ILogger _logger;
    private readonly AppSettings _appSettings;

    public ProcessERP(IEmailNotifier emailNotifier, ILogger logger, AppSettings appSettings)
    {
        _emailNotifier = emailNotifier;
        _logger = logger;
        _appSettings = appSettings;
    }

    public Task<OrderItemModel> GetItemForSupplier(string productEAN, string? supplier)
    {
        var random = new Random();
        return Task.FromResult(new OrderItemModel
        {
            EAN = productEAN,
            UnitPrice = random.Next(0, 1000),
            Quantity = random.Next(0, 1000),
            Description = "Test Description"
        }); 
    }

    public async Task<bool> SendXmlData(HttpClient httpClient, StringContent content, string orderManagementSystemUrl)
    {
        try
        {
            HttpResponseMessage response = await httpClient.PostAsync(orderManagementSystemUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var message = $"Order sent successfully";
                _logger.Information(message);

                await _emailNotifier.SendNotificationAsync(_appSettings.AccountManagerEmail,
                    $"Information | Success Order",
                    message);
            }
            else
            {
                var message = $"Error sending order. Status Code: {response.StatusCode}";
                _logger.Error(message);
                await _emailNotifier.SendNotificationAsync(_appSettings.AccountManagerEmail,
                    $"Error | Order - ",
                    $"{message};");
            }
        }
        catch(Exception ex)
        {
            //Intentionally left empty as don't want to handle this
        }
        

        return true;
    }

    public Task<bool> UpdateStockForItem(string productEAN, int quantity)
    {
        return Task.FromResult(true);
    }
}
