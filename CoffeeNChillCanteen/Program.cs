using CoffeeNChillCanteen.Repositories;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var connectionString = builder.Configuration["AzureWebJobsStorage"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "AzureWebJobsStorage is not configured.");
}

builder.Services.AddSingleton<IMenuRepository>(
    new TableMenuRepository(connectionString));

builder.Build().Run();