using System;
using System.Collections.Generic;
using System.Text;

namespace CoffeeNChillCanteen.Models;

public class MenuItem
{
    public string Category { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public bool IsAvailable { get; set; }
}