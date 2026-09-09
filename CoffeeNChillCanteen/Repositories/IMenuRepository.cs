using System;
using System.Collections.Generic;
using System.Text;
using CoffeeNChillCanteen.Models;

namespace CoffeeNChillCanteen.Repositories;

public interface IMenuRepository
{
    Task<MenuItem> CreateAsync(MenuItem menuItem);

    Task<MenuItem?> GetByIdAsync(
        string category,
        string sku);

    Task<IReadOnlyList<MenuItem>> GetAllAsync();

    Task<IReadOnlyList<MenuItem>> GetByCategoryAsync(
        string category);

    Task<MenuItem?> UpdateAsync(
        MenuItem menuItem);
}