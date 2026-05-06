using System;

namespace AuthX.Core.DTOs.ProductConditions;

public class ProductConditionDto
{
    public int      ProductConditionId { get; set; }
    public string   Name               { get; set; } = null!;
    public string?  Description        { get; set; }
    public bool     IsActive           { get; set; }
    public DateTime CreatedAt          { get; set; }
}

public class CreateProductConditionDto
{
    public string  Name        { get; set; } = null!;
    public string? Description { get; set; }
}

public class UpdateProductConditionDto : CreateProductConditionDto { }