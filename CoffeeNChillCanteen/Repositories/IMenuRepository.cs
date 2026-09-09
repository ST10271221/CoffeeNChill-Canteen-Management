using System;
using System.Collections.Generic;
using System.Text;
using CoffeeNChillCanteen.Models;

namespace CoffeeNChillCanteen.Repositories;

public interface IMenuRepository
{
    Task<MenuItem> CreateAsync(MenuItem menuItem);

    Task<IReadOnlyList<MenuItem>> GetAllAsync();

    Task<IReadOnlyList<MenuItem>> GetByCategoryAsync(string category);

    Task<MenuItem?> GetByIdAsync(string category, string sku);

    Task<MenuItem?> UpdateAsync(MenuItem menuItem);

    Task<bool> DeleteAsync(string category, string sku);
}