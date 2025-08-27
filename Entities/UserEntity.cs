using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserMicroService.Entities;

public class UserEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Required]
    public long UserId { get; set; }
    [Required]
    [MaxLength(20)] 
    public string UserName { get; set; }=string.Empty;
    [Required]
    [MaxLength(40)]
    public string SubId { get; set; }=string.Empty;
    [Required]
    [MaxLength(100)] 
    public string Email { get; set; }=string.Empty;
    [MaxLength(512)]
    public string PictureUrl{ get; set; }=string.Empty;
    [MaxLength(50)] 
    public string? Name { get; set; }
    [Required]
    public byte Provider { get; set; } = 0;

    [Required] public byte Roles { get; set; } = 0;
    [Required] 
    public DateTime CreatedAt { get; set; }
    [Required] 
    public DateTime LastLogin { get; set; }
    [MaxLength(40)]
    public string? ConnectionId { get; set; }
    public int RetryCount { get; set; } = 0;
    [NotMapped]
    public string CorrelationId { get; set; }=String.Empty;
}