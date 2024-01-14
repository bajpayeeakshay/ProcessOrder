using Serilog;
using ProcessOrder.Services.Models.Settings;
using System.IO;
using System.Text;

namespace ProcessOrder.Services.Services;

public class OrderProcessor : IOrderProcessor
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IProcessOrderService _processOrderService;
    private readonly AppSettings _appSettings;
    private readonly HttpClient _httpClient;
    private readonly IEmailNotifier _emailNotifier;
    private readonly IProcessERP _processERP;

    public OrderProcessor(ILogger logger,
        IHttpClientFactory httpClientFactory,
        IProcessOrderService processOrderService,
        AppSettings appSettings, 
        HttpClient httpClient,
        IEmailNotifier emailNotifier,
        IProcessERP processERP)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _processOrderService = processOrderService;
        _appSettings = appSettings;
        _httpClient = httpClient;
        _emailNotifier = emailNotifier;
        _processERP = processERP;
    }

    public async Task ProcessOrderAsync()
    {
        try
        {
            string[] orderData = await File.ReadAllLinesAsync(_appSettings.FilePath);

            if (!_processOrderService.IsFileValid(orderData))
            {
                var errorMessage = $"Invalid Order Data Received - {orderData}";
                await _emailNotifier.SendNotificationAsync(_appSettings.AccountManagerEmail,
                    $"Error | Invalid Order - {DateTime.Now.Date}",
                    errorMessage);
                _logger.Error(errorMessage);
            }
            else
            {
                var order = await _processOrderService.GetOrderFromDataAsync(orderData);

                string xmlRepresentation = _processOrderService.CreateXmlForOrderData(order);

                if (xmlRepresentation != null && xmlRepresentation.Length > 0)
                {
                    var content = new StringContent(xmlRepresentation);

                    //Intentionally handled this way to bypass httpSend just for assessment purpose
                    var result = await _processERP.SendXmlData(_httpClient, content, _appSettings.OrderManagementSystemUrl);

                    using (FileStream fs = File.Create($"{_appSettings.XmlFilePath}load-{DateTime.Now.Millisecond.ToString()}.xml"))
                    {
                        byte[] info = new UTF8Encoding(true).GetBytes(xmlRepresentation);
                        fs.Write(info, 0, info.Length);
                    }
                }
                else
                {
                    _logger.Error($"XML couldn't be created for order {order.OrderNumber}");
                }
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"An error occurred: {ex.Message}";
            _logger.Error(errorMessage);
            await _emailNotifier.SendNotificationAsync(_appSettings.AccountManagerEmail,
                            $"Error | Order",
                            $"{errorMessage};");
        }
    }
}
