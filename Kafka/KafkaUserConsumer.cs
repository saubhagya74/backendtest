using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using UserMicroService.Data;
using UserMicroService.Entities;
using EFCore.BulkExtensions;

namespace UserMicroService.Kafka;
public class KafkaUserConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public KafkaUserConsumer(IServiceScopeFactory scopeFactory, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _config = config;
    }
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _config["Kafka:BootstrapServers"] ?? "localhost:29092",
                GroupId = "user-service-consumer",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
            consumer.Subscribe("users-topic");

            var batch = new List<UserEntity>();
            var batchInterval = TimeSpan.FromMilliseconds(500);
            var lastFlush = DateTime.UtcNow;

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var cr = consumer.Consume(TimeSpan.FromMilliseconds(80));
                        if (cr?.Message?.Value != null)
                        {
                            try
                            {
                                var user = JsonSerializer.Deserialize<UserEntity>(cr.Message.Value, _jsonOptions);
                                if (user != null) batch.Add(user);
                            }
                            catch (JsonException ex)
                            {
                                Console.WriteLine($"❌ Failed to deserialize message: {ex.Message}");
                            }
                        }

                        // Flush batch every batchInterval
                        if ((DateTime.UtcNow - lastFlush) >= batchInterval && batch.Count > 0)
                        {
                            var batchToFlush = batch.ToList();
                            batch.Clear();
                            _ = Task.Run(() => FlushBatchInsertOnly(batchToFlush, stoppingToken, consumer));
                            lastFlush = DateTime.UtcNow;
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        Console.WriteLine($"❌ Kafka consume error: {ex.Error.Reason}");
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Unexpected error: {ex.Message}");
                    }
                }

                // Final flush on shutdown
                if (batch.Count > 0) await FlushBatchInsertOnly(batch, stoppingToken, consumer);
            }
            finally
            {
                consumer.Close();
                Console.WriteLine("🛑 Kafka consumer stopped.");
            }

        }, stoppingToken);
    }

    private async Task FlushBatchInsertOnly(List<UserEntity> batch, CancellationToken ct, IConsumer<Ignore, string> consumer)
    {
        if (batch.Count == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var producer = scope.ServiceProvider.GetRequiredService<IKafkaProducer>(); 

        try
        {
            // Disable change tracking for performance
            db.ChangeTracker.AutoDetectChangesEnabled = false;

            // Insert only: skip duplicates
            await db.BulkInsertAsync(batch, bulkConfig =>
            {
                bulkConfig.PreserveInsertOrder = true;
                bulkConfig.SetOutputIdentity = true; // key option for insert-only
            }, null, null, ct);

            consumer.Commit(); // commit after successful insert
            // Console.WriteLine($"Flushed{batch.Count} users at {DateTime.UtcNow:HH:mm:ss}");
            Console.Write($"Flushed->{batch.Count}||");
            // Create batch notification
            var correlationIds = batch
                .Where(u => !string.IsNullOrEmpty(u.CorrelationId))
                .Select(u => u.CorrelationId)
                .ToList();
            
            if (correlationIds.Count > 0)
            {
                var notificationBatch = new
                {
                    CorrelationIds = correlationIds,
                    Status = "Success",
                    InsertedAt = DateTime.UtcNow
                };

                _ = producer.ProduceAsync("notification-topic", notificationBatch);
                Console.WriteLine($"Notification->{correlationIds.Count}");
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Batch insert failed: {ex.Message}");
            // Optional: log batch for retry
        }
        finally
        {
            batch.Clear();
        }
    }
}
