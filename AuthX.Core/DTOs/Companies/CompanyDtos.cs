// AuthX.Core/DTOs/Companies/CompanyDtos.cs

namespace AuthX.Core.DTOs.Companies;

public class CompanyListDto
{
    public int     CompanyId      { get; set; }
    public string  Name           { get; set; } = null!;
    public string? Domain         { get; set; }
    public string? LogoUrl        { get; set; }
    public bool    IsActive       { get; set; }
    public bool    IsOwnerCompany { get; set; }
    public DateTime CreatedAt     { get; set; }
    public int     UserCount      { get; set; }
}

public class CreateCompanyDto
{
    public string  Name     { get; set; } = null!;
    public string? Domain   { get; set; }
    public string? LogoUrl  { get; set; }
}

public class UpdateCompanyDto
{
    public string  Name    { get; set; } = null!;
    public string? Domain  { get; set; }
    public string? LogoUrl { get; set; }
}