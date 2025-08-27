using Confluent.Kafka;
using System.Text.Json;

namespace UserMicroService.Kafka
{
    public class UserConsumer : BackgroundService
    {
        private readonly IConfiguration _config;
        private readonly NotificationHandler _notificationHandler; // handles TaskCompletionSource

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public UserConsumer(IConfiguration config, NotificationHandler notificationHandler)
        {
            _config = config;
            _notificationHandler = notificationHandler;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() =>
            {
                var consumerConfig = new ConsumerConfig
                {
                    BootstrapServers = _config["Kafka:BootstrapServers"] ?? "localhost:29092",
                    GroupId = "notification-consumer",
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = true
                };

                using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
                consumer.Subscribe("notification-topic");

                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var cr = consumer.Consume(TimeSpan.FromMilliseconds(200));
                        if (cr?.Message?.Value != null)
                        {
                            try
                            {
                                var notification = JsonSerializer.Deserialize<NotificationBatch>(cr.Message.Value, _jsonOptions);
                                if (notification != null)
                                {
                                    // Notify the waiting HTTP requests
                                    _notificationHandler.HandleNotification(notification);
                                }
                            }
                            catch (JsonException ex)
                            {
                                Console.WriteLine($"❌ Failed to deserialize notification: {ex.Message}");
                            }
                        }
                    }
                }
                finally
                {
                    consumer.Close();
                    Console.WriteLine("🛑 Notification consumer stopped.");
                }

            }, stoppingToken);
        }
    }

    // Model for batch notifications
    public class NotificationBatch
    {
        public List<string> CorrelationIds { get; set; } = new();
        public string Status { get; set; } = "Success";
        public DateTime InsertedAt { get; set; }
    }
}
