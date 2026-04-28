namespace AuthX.Core.DTOs.Settings;

public class PrintSettingsDto
{
    public decimal LabelWidthMm    { get; set; } = 17;
    public decimal LabelHeightMm   { get; set; } = 30;
    public decimal QRSizeMm        { get; set; } = 17;
    public int     ColumnsPerRow   { get; set; } = 1;
    public bool    ShowProductName { get; set; } = true;
    public bool    ShowSerialNo    { get; set; } = true;
    public bool    ShowBatchNo     { get; set; } = true;
    public bool    ShowColorName   { get; set; } = false;
    public bool    ShowModelNo     { get; set; } = false;
    public bool    ShowCompanyName { get; set; } = false;
    public int     WarrantyDelayDays { get; set; } = 60;
}