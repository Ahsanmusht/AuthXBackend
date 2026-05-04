using AuthX.Core.DTOs.Colors;

namespace AuthX.Core.DTOs.Products;

public class ProductListDto
{
    public int     ProductId    { get; set; }
    public int     CategoryId   { get; set; }
    public string  Name         { get; set; } = null!;
    public string  SKU          { get; set; } = null!;
    public string  CategoryName { get; set; } = null!;
    public int     WarrantyDays { get; set; }
    public bool    IsActive     { get; set; }
    public DateTime CreatedAt   { get; set; }
    public string? ModelNo  { get; set; }
    public string? Description  { get; set; }
    public string? ImageUrl { get; set; }
    public List<ColorDto> Colors { get; set; } = new();
}

public class ProductDetailDto : ProductListDto
{
    public int     CategoryId  { get; set; }
    public string? Description { get; set; }
}

public class CreateProductDto
{
    public int     CategoryId   { get; set; }
    public string  Name         { get; set; } = null!;
    public string  SKU          { get; set; } = null!;
    public int     WarrantyDays { get; set; } = 365;
    public string? Description  { get; set; }
    public string?    ModelNo    { get; set; }
    public string?    ImageUrl   { get; set; }  // base64 ya URL
    public List<int>  ColorIds   { get; set; } = new();
}

public class UpdateProductDto
{
    public int     CategoryId   { get; set; }
    public string  Name         { get; set; } = null!;
    public int     WarrantyDays { get; set; }
    public string? Description  { get; set; }
    public string?    ModelNo    { get; set; }
    public string?    ImageUrl   { get; set; }
    public List<int>  ColorIds   { get; set; } = new();
}