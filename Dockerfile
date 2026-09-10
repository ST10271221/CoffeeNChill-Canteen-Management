# CoffeeNChill Azure Functions - optimized container image
# .NET 10 isolated worker

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY CoffeeNChillCanteen/CoffeeNChillCanteen.csproj CoffeeNChillCanteen/

RUN dotnet restore CoffeeNChillCanteen/CoffeeNChillCanteen.csproj \
    -p:RestoreFallbackFolders=""

COPY CoffeeNChillCanteen/ CoffeeNChillCanteen/

RUN dotnet publish CoffeeNChillCanteen/CoffeeNChillCanteen.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    -p:RestoreFallbackFolders=""

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0 AS runtime

WORKDIR /home/site/wwwroot

COPY --from=build /app/publish/ .

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true