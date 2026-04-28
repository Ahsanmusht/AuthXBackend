namespace AuthX.Core.DTOs.Colors;

public class ColorDto
{
    public int    ColorId  { get; set; }
    public string Name     { get; set; } = null!;
    public string HexCode  { get; set; } = null!;
    public bool   IsActive { get; set; }
}

public class CreateColorDto
{
    public string Name    { get; set; } = null!;
    public string HexCode { get; set; } = null!;
}