namespace AuthX.Core.DTOs.Notifications;

public class NotificationDto
{
    public long     NotificationId { get; set; }
    public string   Type           { get; set; } = null!;
    public string   Message        { get; set; } = null!;
    public long?    ReferenceId    { get; set; }
    public string?  ActionUrl      { get; set; }
    public bool     IsRead         { get; set; }
    public DateTime CreatedAt      { get; set; }
}