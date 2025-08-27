using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scalar.AspNetCore;
using UserMicroService.Data;
using UserMicroService.Kafka;
using UserMicroService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();
//entity
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DatabaseConnection")));
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
//dapper
var connectionString = builder.Configuration.GetConnectionString("DatabaseConnection");
builder.Services.AddScoped<IDbConnection>(sp => new NpgsqlConnection(connectionString));
//snowflake
builder.Services.AddSingleton<SnowFlakeGen>();
//kafka
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddHostedService<KafkaUserConsumer>();
builder.Services.AddHostedService<UserConsumer>();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);
//singleton
builder.Services.AddSingleton<NotificationHandler>();
// Also register NotificationConsumer as hosted service
//Build App
var app = builder.Build();
// Configure the HTTP request pipeline.
app.MapControllers();
    app.MapOpenApi();
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "UserMicroService");
    });
}
app.MapScalarApiReference();
app.UseHttpsRedirection();
app.Run();
