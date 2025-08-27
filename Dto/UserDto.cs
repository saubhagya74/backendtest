namespace UserMicroService.Dto;

public class UserDto
{ 
    public string UserName { get; set; }=string.Empty;
    public string SubId { get; set; }=string.Empty;
    public string Email { get; set; }=string.Empty;
    public string PictureUrl{ get; set; }=string.Empty;
    public string? Name { get; set; }
    public byte Provider { get; set; } = 0;
    public byte Roles { get; set; }
}