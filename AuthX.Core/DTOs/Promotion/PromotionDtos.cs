namespace AuthX.Core.DTOs.Promotion;
 
public class PromotionDto
{
    public int      PromotionId { get; set; }
    public string?  Title       { get; set; }
    public string   ImageUrl    { get; set; } = null!;
    public string?  ForwardUrl  { get; set; }
    public bool     IsActive    { get; set; }
    public DateTime CreatedAt   { get; set; }
    public DateTime? UpdatedAt  { get; set; }
}
 
public class CreatePromotionDto
{
    public string?  Title      { get; set; }
    public string   ImageUrl   { get; set; } = null!;
    public string?  ForwardUrl { get; set; }
}
 
public class UpdatePromotionDto : CreatePromotionDto { }