using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using CoffeeNChillCanteen.DTOs;
using CoffeeNChillCanteen.Models;
using CoffeeNChillCanteen.Repositories;
using Microsoft.Azure.Functions.Worker.Http;


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

            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            var validationContext =
                new System.ComponentModel.DataAnnotations.ValidationContext(request);

            if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
                    request,
                    validationContext,
                    validationResults,
                    validateAllProperties: true))
            {
                return await CreateValidationErrorResponse(
                    req,
                    validationResults);
            }

            var existingItem = await _menuRepository.GetByIdAsync(
                request.Category,
                request.Sku);

            if (existingItem is not null)
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.Conflict,
                    $"Menu item with SKU '{request.Sku}' already exists in category '{request.Category}'.");
            }

            var menuItem = new MenuItem
            {
                Category = request.Category.Trim(),
                Sku = request.Sku.Trim().ToUpperInvariant(),
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
            var menuItems = await _menuRepository.GetAllAsync();

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
        List<System.ComponentModel.DataAnnotations.ValidationResult> validationResults)
    {
        var response = req.CreateResponse(HttpStatusCode.BadRequest);

        await response.WriteAsJsonAsync(new
        {
            error = "Validation failed.",
            details = validationResults
                .SelectMany(result =>
                    result.MemberNames.Select(memberName => new
                    {
                        field = memberName,
                        message = result.ErrorMessage
                    }))
                .ToList()
        });

        return response;
    }
}