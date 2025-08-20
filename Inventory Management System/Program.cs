using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Azure;
using Microsoft.Azure.Cosmos;
using Provider;

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
        var serviceBusConnection = config.GetValue<string>("salespro:fullyQualifiedNamespace");

        var credentialOptions = new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = managedIdentityClientId
        };

        services.AddAzureClients(builder =>
        {
            builder.AddServiceBusClientWithNamespace(serviceBusConnection)
                   .WithCredential(new DefaultAzureCredential(credentialOptions));
        });

        services.AddSingleton<ICosmosDbProvider, CosmosDbProvider>(sp =>
        {
            var cosmosAccountUri = config.GetValue<string>("CosmosAccountUri");
            var database = config.GetValue<string>("CosmosDbName");
            var tokenCredential = new DefaultAzureCredential();
            var cosmosClient = new CosmosClient(accountEndpoint: cosmosAccountUri, tokenCredential, new CosmosClientOptions
            {
                MaxRetryAttemptsOnRateLimitedRequests = 5,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(45)
            });

            return new CosmosDbProvider(cosmosClient, database, config);
        });
    })
    .Build();

host.Run();
