namespace UserMicroService.Dto2;

public class UserDto2
{ 
    public long UserId { get; set; } 
    public string UserName { get; set; }=string.Empty;
    public string SubId { get; set; }=string.Empty;
    public string Email { get; set; }=string.Empty;
    public string PictureUrl{ get; set; }=string.Empty;
    public string? Name { get; set; }
    public byte Provider { get; set; } = 0;
    public byte Roles { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime LastLogin { get; set; }
    public string? ConnectionId { get; set; }
    public int RetryCount { get; set; } = 0;
    public string CorrelationId { get; set; }=String.Empty;
}