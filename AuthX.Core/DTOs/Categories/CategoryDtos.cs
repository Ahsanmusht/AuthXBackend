namespace AuthX.Core.DTOs.Categories;

public class CategoryDto
{
    public int     CategoryId  { get; set; }
    public string  Name        { get; set; } = null!;
    public string? Description { get; set; }
    public bool    IsActive    { get; set; }
    public DateTime CreatedAt  { get; set; }
}

public class CreateCategoryDto
{
    public string  Name        { get; set; } = null!;
    public string? Description { get; set; }
}

public class UpdateCategoryDto : CreateCategoryDto { }