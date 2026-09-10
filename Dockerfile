# CoffeeNChill Azure Functions - baseline container image
# .NET 10 isolated worker

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0

WORKDIR /home/site/wwwroot

COPY CoffeeNChillCanteen/bin/Release/net10.0/ .

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true