using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CoffeeNChillCanteen.DTOs;

public class UpdateMenuItemRequest : IValidatableObject
{
    [Range(
        typeof(decimal),
        "0.01",
        "100000.00",
        ErrorMessage = "Price must be between 0.01 and 100000.00."
    )]
    public decimal? Price { get; set; }

    public bool? IsAvailable { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (Price is null && IsAvailable is null)
        {
            yield return new ValidationResult(
                "At least one property must be supplied for an update.",
                new[] { nameof(Price), nameof(IsAvailable) }
            );
        }
    }
}
