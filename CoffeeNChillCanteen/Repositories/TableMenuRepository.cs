using Azure;
using Azure.Data.Tables;
using CoffeeNChillCanteen.Models;
using System;
using System.Collections.Generic;
using System.Text;


namespace CoffeeNChillCanteen.Repositories;

public class TableMenuRepository : IMenuRepository
{
    private readonly TableClient _tableClient;

    public TableMenuRepository(string connectionString)
    {
        _tableClient = new TableClient(
            connectionString,
            "MenuItems"
        );
    }

    public async Task<MenuItem> CreateAsync(MenuItem menuItem)
    {
        await _tableClient.CreateIfNotExistsAsync();

        var entity = ToEntity(menuItem);

        await _tableClient.AddEntityAsync(entity);

        return ToModel(entity);
    }

    public async Task<IReadOnlyList<MenuItem>> GetAllAsync()
    {
        await _tableClient.CreateIfNotExistsAsync();

        var results = new List<MenuItem>();

        await foreach (var entity in _tableClient.QueryAsync<MenuItemEntity>())
        {
            results.Add(ToModel(entity));
        }

        return results;
    }

    public async Task<IReadOnlyList<MenuItem>> GetByCategoryAsync(
        string category)
    {
        await _tableClient.CreateIfNotExistsAsync();

        var results = new List<MenuItem>();

        await foreach (var entity in _tableClient.QueryAsync<MenuItemEntity>(
            filter: x => x.PartitionKey == category))
        {
            results.Add(ToModel(entity));
        }

        return results;
    }

    public async Task<MenuItem?> GetByIdAsync(
        string category,
        string sku)
    {
        await _tableClient.CreateIfNotExistsAsync();

        try
        {
            var response = await _tableClient.GetEntityAsync<MenuItemEntity>(
                category,
                sku);

            return ToModel(response.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<MenuItem?> UpdateAsync(MenuItem menuItem)
    {
        await _tableClient.CreateIfNotExistsAsync();

        try
        {
            var entity = ToEntity(menuItem);

            await _tableClient.UpdateEntityAsync(
                entity,
                ETag.All,
                TableUpdateMode.Replace);

            return ToModel(entity);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(
        string category,
        string sku)
    {
        await _tableClient.CreateIfNotExistsAsync();

        try
        {
            await _tableClient.DeleteEntityAsync(
                category,
                sku);

            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    private static MenuItemEntity ToEntity(MenuItem model)
    {
        return new MenuItemEntity
        {
            PartitionKey = model.Category,
            RowKey = model.Sku,
            Name = model.Name,
            Description = model.Description,
            Price = (double)model.Price,
            IsAvailable = model.IsAvailable
        };
    }

    private static MenuItem ToModel(MenuItemEntity entity)
    {
        return new MenuItem
        {
            Category = entity.PartitionKey,
            Sku = entity.RowKey,
            Name = entity.Name,
            Description = entity.Description,
            Price = (decimal)entity.Price,
            IsAvailable = entity.IsAvailable
        };
    }
}
