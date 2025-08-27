using Microsoft.EntityFrameworkCore;
using UserMicroService.Entities;

namespace UserMicroService.Data;

public class AppDbContext:DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options):base(options)
    {  
    }
    public DbSet<UserEntity> Users { get; set; }
}