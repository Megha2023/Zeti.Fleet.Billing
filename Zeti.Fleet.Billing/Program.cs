using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zeti.Fleet.Billing;
using Zeti.Fleet.Billing.Model;
using Zeti.Fleet.Billing.Services;
using Zeti.Fleet.Billing.Validator;

const string localSettingFileName = "local.settings.json";
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile(localSettingFileName, optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((_, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices(( services) =>
    {
        services.Configure<BillingOptions>(configuration.GetSection("Billing"));
        services.AddHttpClient<IBillingService, BillingService>();
        services.AddScoped<IValidator<BillingRequest>, BillingValidator>();
        services.AddSingleton<IBillFormatter, JsonBillFormatter>();
        services.AddSingleton<BillFormatterFactory>();
        services.AddLogging();
    })
    .Build();

host.Run();