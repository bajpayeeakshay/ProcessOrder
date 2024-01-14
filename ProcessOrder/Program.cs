using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProcessOrder.Common;
using ProcessOrder.Services.Models.Settings;
using ProcessOrder.Services.Services;
using Serilog;
using System.Reflection.Metadata.Ecma335;

namespace ProcessOrder;

public class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Constants.AppSettingsFileName, false, true)
            .AddJsonFile(Constants.FileFormatSettingsFilename, false, true)
            .Build();

        var serviceProvider = ConfigureServices(configuration);
        var orderProcessor = serviceProvider.GetRequiredService<OrderProcessor>();

        // Process the order
        await orderProcessor.ProcessOrderAsync();
    }

    static IServiceProvider ConfigureServices(IConfiguration configuration)
    {
        var test = configuration.GetSection(Constants.SmtpSettingsSection).Get<SmtpSettings>();
        var appSettings = configuration.GetSection(Constants.AppSettingsSection)
            .Get<AppSettings>();

        var serviceProvider = new ServiceCollection();
        var logger = new Serilog.LoggerConfiguration()
            .WriteTo.File("Logs/ProcessOrder.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Serilog.Log.Logger = logger;
        serviceProvider.AddSingleton<ILogger>(logger);

        serviceProvider.AddHttpClient<IOrderProcessor, OrderProcessor> (
            client =>
            {
                client.BaseAddress = new Uri(appSettings?.OrderManagementSystemUrl);
                client.DefaultRequestHeaders.Accept.Clear();
            });

        serviceProvider.AddSingleton<OrderProcessor>();
        serviceProvider.AddTransient<IProcessOrderService, ProcessOrderService>();
        serviceProvider.AddTransient<IProcessERP, ProcessERP>();
        serviceProvider.AddTransient<IEmailNotifier, EmailNotifier>();
        serviceProvider.AddSingleton<AppSettings>(appSettings);
        serviceProvider.AddSingleton<SmtpSettings>(configuration.GetSection(Constants.SmtpSettingsSection)
            .Get<SmtpSettings>());
        serviceProvider.AddSingleton<FileFormatSettings>(configuration.GetSection(Constants.FileFormatSettingsSection)
            .Get<FileFormatSettings>());

        return serviceProvider.BuildServiceProvider();
    }
}
