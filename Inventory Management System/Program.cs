using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Azure;
using Microsoft.Azure.Cosmos;
using Provider;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.WebJobs.ServiceBus;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((context, configBuilder) =>
    {
        configBuilder.SetBasePath(Directory.GetCurrentDirectory())
                     .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                     .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables();
        configBuilder.Build();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetry();
        services.ConfigureFunctionsApplicationInsights();

        var config = context.Configuration;
        var managedIdentityClientId = config.GetValue<string>("sales-pro-managed-indentity");
        var serviceBusConnection = config.GetValue<string>("ServiceBusNamespace");

        var credentialOptions = new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = managedIdentityClientId
        };

        services.AddAzureClients(builder =>
        {
            builder.AddServiceBusClientWithNamespace(serviceBusConnection)
                   .WithCredential(new DefaultAzureCredential(credentialOptions));
        });

        // Registering cosmos db through Database Context
        var cosmosAccountUri = config.GetValue<string>("CosmosAccountUri");
        var database = config.GetValue<string>("CosmosDbName");
        var tokenCredential = new DefaultAzureCredential(credentialOptions);

        services.AddDbContext<InventoryDbContext>(options =>
        {
            options.UseCosmos(
                accountEndpoint: cosmosAccountUri,
                tokenCredential: tokenCredential,
                databaseName: database);
        });
        services.AddScoped<ICosmosDbProvider, CosmosDbProvider>();



    })
    .Build();

host.Run();
