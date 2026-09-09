using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CoffeeNChillCanteen.DTOs;

public class CreateMenuItemRequest
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [RegularExpression(
        @"^[A-Z]{3}-\d{3,}$",
        ErrorMessage = "SKU must follow the format ABC-123."
    )]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Description { get; set; } = string.Empty;

    [Range(
        typeof(decimal),
        "0.01",
        "100000.00",
        ErrorMessage = "Price must be between 0.01 and 100000.00."
    )]
    public decimal Price { get; set; }

    public bool IsAvailable { get; set; } = true;
}
