using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using CoffeeNChillCanteen.DTOs;
using CoffeeNChillCanteen.Models;
using CoffeeNChillCanteen.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillCanteen;

public class MenuFunctions
{
    private readonly IMenuRepository _menuRepository;
    private readonly ILogger<MenuFunctions> _logger;

    public MenuFunctions(
        IMenuRepository menuRepository,
        ILogger<MenuFunctions> logger)
    {
        _menuRepository = menuRepository;
        _logger = logger;
    }

    [Function("CreateMenuItem")]
    public async Task<HttpResponseData> CreateMenuItem(
        [HttpTrigger(
            AuthorizationLevel.Function,
            "post",
            Route = "menu")]
        HttpRequestData req)
    {
        try
        {
            var request = await JsonSerializer.DeserializeAsync<CreateMenuItemRequest>(
                req.Body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (request is null)
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.BadRequest,
                    "Request body is required.");
            }

            var validationResults = ValidateRequest(request);

            if (validationResults.Count > 0)
            {
                return await CreateValidationErrorResponse(
                    req,
                    validationResults);
            }

            var category = request.Category.Trim();
            var sku = request.Sku.Trim().ToUpperInvariant();

            var existingItem = await _menuRepository.GetByIdAsync(
                category,
                sku);

            if (existingItem is not null)
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.Conflict,
                    $"Menu item with SKU '{sku}' already exists in category '{category}'.");
            }

            var menuItem = new MenuItem
            {
                Category = category,
                Sku = sku,
                Name = request.Name.Trim(),
                Description = request.Description.Trim(),
                Price = request.Price,
                IsAvailable = request.IsAvailable
            };

            var createdItem = await _menuRepository.CreateAsync(menuItem);

            _logger.LogInformation(
                "Created menu item {Sku} in category {Category}.",
                createdItem.Sku,
                createdItem.Category);

            var response = req.CreateResponse(HttpStatusCode.Created);

            await response.WriteAsJsonAsync(createdItem);

            return response;
        }
        catch (JsonException)
        {
            return await CreateErrorResponse(
                req,
                HttpStatusCode.BadRequest,
                "Invalid JSON request body.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while creating menu item.");

            return await CreateErrorResponse(
                req,
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred while creating the menu item.");
        }
    }

    [Function("GetAllMenuItems")]
    public async Task<HttpResponseData> GetAllMenuItems(
        [HttpTrigger(
            AuthorizationLevel.Function,
            "get",
            Route = "menu")]
        HttpRequestData req)
    {
        try
        {
            var category = GetQueryParameter(
                req.Url.Query,
                "category");

            IReadOnlyList<MenuItem> menuItems;

            if (category is null)
            {
                menuItems = await _menuRepository.GetAllAsync();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(category))
                {
                    return await CreateErrorResponse(
                        req,
                        HttpStatusCode.BadRequest,
                        "Category cannot be empty.");
                }

                menuItems = await _menuRepository.GetByCategoryAsync(
                    category.Trim());
            }

            var response = req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(menuItems);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while retrieving menu items.");

            return await CreateErrorResponse(
                req,
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred while retrieving menu items.");
        }
    }

    [Function("GetMenuItemsByCategory")]
    public async Task<HttpResponseData> GetMenuItemsByCategory(
        [HttpTrigger(
            AuthorizationLevel.Function,
            "get",
            Route = "menu/category/{category}")]
        HttpRequestData req,
        string category)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.BadRequest,
                    "Category is required.");
            }

            category = category.Trim();

            var menuItems = await _menuRepository.GetByCategoryAsync(
                category);

            _logger.LogInformation(
                "Retrieved {Count} menu items from category {Category}.",
                menuItems.Count,
                category);

            var response = req.CreateResponse(
                HttpStatusCode.OK);

            await response.WriteAsJsonAsync(menuItems);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while retrieving menu items from category {Category}.",
                category);

            return await CreateErrorResponse(
                req,
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred while retrieving menu items by category.");
        }
    }

    [Function("UpdateMenuItem")]
    public async Task<HttpResponseData> UpdateMenuItem(
        [HttpTrigger(
            AuthorizationLevel.Function,
            "put",
            Route = "menu/{category}/{sku}")]
        HttpRequestData req,
        string category,
        string sku)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category) ||
                string.IsNullOrWhiteSpace(sku))
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.BadRequest,
                    "Category and SKU are required.");
            }

            category = category.Trim();
            sku = sku.Trim().ToUpperInvariant();

            var request = await JsonSerializer.DeserializeAsync<UpdateMenuItemRequest>(
                req.Body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (request is null)
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.BadRequest,
                    "Request body is required.");
            }

            var validationResults = ValidateRequest(request);

            if (validationResults.Count > 0)
            {
                return await CreateValidationErrorResponse(
                    req,
                    validationResults);
            }

            var existingItem = await _menuRepository.GetByIdAsync(
                category,
                sku);

            if (existingItem is null)
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.NotFound,
                    $"Menu item with SKU '{sku}' was not found in category '{category}'.");
            }

            if (request.Price.HasValue)
            {
                existingItem.Price = request.Price.Value;
            }

            if (request.IsAvailable.HasValue)
            {
                existingItem.IsAvailable = request.IsAvailable.Value;
            }

            var updatedItem = await _menuRepository.UpdateAsync(
                existingItem);

            if (updatedItem is null)
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.NotFound,
                    $"Menu item with SKU '{sku}' was not found in category '{category}'.");
            }

            _logger.LogInformation(
                "Updated menu item {Sku} in category {Category}.",
                updatedItem.Sku,
                updatedItem.Category);

            var response = req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(updatedItem);

            return response;
        }
        catch (JsonException)
        {
            return await CreateErrorResponse(
                req,
                HttpStatusCode.BadRequest,
                "Invalid JSON request body.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while updating menu item {Sku} in category {Category}.",
                sku,
                category);

            return await CreateErrorResponse(
                req,
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred while updating the menu item.");
        }
    }

    [Function("DeleteMenuItem")]
    public async Task<HttpResponseData> DeleteMenuItem(
    [HttpTrigger(AuthorizationLevel.Function,"delete",Route = "menu/{category}/{sku}")]
    HttpRequestData req,
    string category,
    string sku)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category) ||
                string.IsNullOrWhiteSpace(sku))
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.BadRequest,
                    "Category and SKU are required.");
            }

            category = category.Trim();
            sku = sku.Trim().ToUpperInvariant();

            var existingItem = await _menuRepository.GetByIdAsync(
                category,
                sku);

            if (existingItem is null)
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.NotFound,
                    $"Menu item with SKU '{sku}' was not found in category '{category}'.");
            }

            var deleted = await _menuRepository.DeleteAsync(
                category,
                sku);

            if (!deleted)
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.NotFound,
                    $"Menu item with SKU '{sku}' was not found in category '{category}'.");
            }

            _logger.LogInformation(
                "Deleted menu item {Sku} from category {Category}.",
                sku,
                category);

            return req.CreateResponse(
                HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while deleting menu item {Sku} in category {Category}.",
                sku,
                category);

            return await CreateErrorResponse(
                req,
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred while deleting the menu item.");
        }
    }

    private static List<ValidationResult> ValidateRequest(
    object request)
    {
        var validationResults = new List<ValidationResult>();

        var validationContext = new ValidationContext(request);

        Validator.TryValidateObject(
            request,
            validationContext,
            validationResults,
            validateAllProperties: true);

        return validationResults;
    }

    private static string? GetQueryParameter(
        string query,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var queryWithoutPrefix = query.TrimStart('?');

        foreach (var parameter in queryWithoutPrefix.Split(
            '&',
            StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = parameter.Split(
                '=',
                2,
                StringSplitOptions.None);

            var name = Uri.UnescapeDataString(
                parts[0].Replace("+", " "));

            if (!name.Equals(
                    parameterName,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (parts.Length == 1)
            {
                return string.Empty;
            }

            return Uri.UnescapeDataString(
                parts[1].Replace("+", " "));
        }

        return null;
    }

    private static async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req,
        HttpStatusCode statusCode,
        string message)
    {
        var response = req.CreateResponse(statusCode);

        await response.WriteAsJsonAsync(new
        {
            error = message
        });

        return response;
    }

    private static async Task<HttpResponseData> CreateValidationErrorResponse(
        HttpRequestData req,
        List<ValidationResult> validationResults)
    {
        var response = req.CreateResponse(HttpStatusCode.BadRequest);

        await response.WriteAsJsonAsync(new
        {
            error = "Validation failed.",
            details = validationResults
                .SelectMany(result =>
                    result.MemberNames.Any()
                        ? result.MemberNames.Select(memberName => new
                        {
                            field = memberName,
                            message = result.ErrorMessage
                        })
                        : new[]
                        {
                            new
                            {
                                field = string.Empty,
                                message = result.ErrorMessage
                            }
                        })
                .ToList()
        });

        return response;
    }
}