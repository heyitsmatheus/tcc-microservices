using Confluent.Kafka;
using OrderProcessor.Kafka.Models;
using System.Text.Json;

namespace OrderProcessor.Kafka.Services;

public class OrderConsumerService : BackgroundService
{
    private readonly ILogger<OrderConsumerService> _logger;
    private readonly string _bootstrapServers;
    private readonly string _topic;
    private readonly string _groupId;

    public OrderConsumerService(ILogger<OrderConsumerService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _bootstrapServers = configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092";
        _topic = configuration["KAFKA_TOPIC"] ?? "orders";
        _groupId = configuration["KAFKA_GROUP_ID"] ?? "order-processor-group";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            AllowAutoCreateTopics = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        consumer.Subscribe(_topic);

        _logger.LogInformation("Consumer started. Listening on topic {Topic}", _topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);

                var order = JsonSerializer.Deserialize<OrderRequest>(result.Message.Value,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (order is null) continue;

                _logger.LogInformation("Processing order {OrderId}", order.OrderId);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
            {
                _logger.LogWarning("Topic '{Topic}' not available yet. Retrying in 2s...", _topic);
                await Task.Delay(2000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming message");
            }
        }

        consumer.Close();
    }
}