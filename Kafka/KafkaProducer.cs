using Confluent.Kafka;
using System.Text.Json;

namespace UserMicroService.Kafka;
public interface IKafkaProducer
{
    Task ProduceAsync<T>(string topic, T message);
}

public class KafkaProducer : IKafkaProducer
{
    private readonly IProducer<Null, string> _producer;

    public KafkaProducer(IConfiguration config)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:29092"
        };
        // Console.WriteLine("made the kafka producer constructor");
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
    }

    public async Task ProduceAsync<T>(string topic, T message)
    {
        var json = JsonSerializer.Serialize(message);
        try
        {
            var result = await _producer.ProduceAsync(topic, new Message<Null, string> { Value = json });
            // Console.WriteLine($"--->>{result.TopicPartitionOffset}");
            // Console.WriteLine($"sent");
        }
        catch (Exception ex)
        {
            // Console.WriteLine($"<--------------- Kafka produce failed: {ex.Message}");
            Console.WriteLine($"<<<<<<<<<<<<,<<- Kafka produce failed->>>>>>>>>>>>>>>");
        }
    }
}
