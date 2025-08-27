using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using UserMicroService.Data;
using UserMicroService.Entities;
using UserMicroService.Kafka;
using UserMicroService.Services;
using UserMicroService.Dto;
using UserMicroService.Dto2;

namespace UserMicroService.Controllers;

[ApiController]
[Route("[controller]")]

public class SignUserController:ControllerBase
{
    private readonly AppDbContext _edb;
    private readonly SnowFlakeGen _idgen;
    private readonly IKafkaProducer _producer;
    private readonly IDbConnection _ddb;
    private readonly NotificationHandler _notificationHandler;
    
    public SignUserController(AppDbContext edb,SnowFlakeGen idgen,
        IKafkaProducer producer, IDbConnection ddb, NotificationHandler notificationHandler)
    {
        _edb = edb;
        _idgen = idgen;
        _producer = producer;
        _ddb = ddb;
        _notificationHandler = notificationHandler;
    }
    
    [HttpPost("createuser")]
    public async Task<IActionResult> CreateUser([FromBody] UserDto user)
    {
        var xuser = new UserEntity()
        {
            UserId = _idgen.GenerateId(),
            SubId = user.SubId,
            UserName = user.UserName,
            Name = user.Name,
            Email = user.Email,
            Roles = user.Roles,
            CreatedAt = DateTime.UtcNow,
            LastLogin = DateTime.UtcNow,
            Provider = user.Provider,
            PictureUrl = user.PictureUrl,
            ConnectionId = null,
            CorrelationId = Guid.NewGuid().ToString()
        };
        await _producer.ProduceAsync("users-topic", xuser);
        // Wait for notification
        var isCompleted = await _notificationHandler.WaitForNotificationAsync(xuser.CorrelationId, TimeSpan.FromSeconds(10));

        if (isCompleted)
            return Ok();
        else
            return StatusCode(504, new { message = "Timeout waiting for insert confirmation" });
    }

    [HttpGet("healthcheck")]
    public async Task<IActionResult> HealthCheck()
    {
        return Ok(new { message = "Healthy" });
    }
    [HttpPost("create")]
    public async Task<IActionResult> CreateUser1([FromBody] UserDto user)
    {
        var xuser = new UserEntity()
        {
            UserId = _idgen.GenerateId(),
            SubId = "1111111111111111111111111111111111111111",
            UserName = user.UserName,
            Name = "Saubhagya Banjade",
            Email = user.Email,
            Roles = 1,
            CreatedAt = DateTime.Now,
            LastLogin = DateTime.Now,
            Provider = 1,
            PictureUrl =
                "111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111",
            ConnectionId = null,
        };
        var Query = @"INSERT INTO ""Users"" 
                    (""UserId"", ""SubId"", ""UserName"", ""Name"", ""Email"", ""Roles"", 
                     ""CreatedAt"", ""LastLogin"", ""Provider"", ""PictureUrl"", ""ConnectionId"")
                    VALUES(@UserId, @SubId, @UserName, @Name, @Email, @Roles, 
                    @CreatedAt, @LastLogin, @Provider, @PictureUrl, @ConnectionId)";
        await _ddb.ExecuteAsync(Query, xuser);
        // await _edb.Users.AddAsync(xuser);
        // await _edb.SaveChangesAsync();
        return Ok(new { message = "user created" });
    }
}