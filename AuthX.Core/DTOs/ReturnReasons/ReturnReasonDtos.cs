namespace AuthX.Core.DTOs.ReturnReasons;

public class ReturnReasonDto
{
    public int      ReturnReasonId { get; set; }
    public string   Name           { get; set; } = null!;
    public string?  Description    { get; set; }
    public bool     IsActive       { get; set; }
    public DateTime CreatedAt      { get; set; }
}

public class CreateReturnReasonDto
{
    public string  Name        { get; set; } = null!;
    public string? Description { get; set; }
}

public class UpdateReturnReasonDto : CreateReturnReasonDto { }