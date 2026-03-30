using System.Diagnostics;
using System.Diagnostics.Metrics;
using Confluent.Kafka;
using OrderProcessor.Kafka.Models;
using System.Text.Json;

namespace OrderProcessor.Kafka.Services;

public class OrderConsumerService(
    ILogger<OrderConsumerService> logger,
    IConfiguration configuration,
    Counter<long> ordersProcessed,
    Histogram<double> processingTime) : BackgroundService
{
    private readonly string _bootstrapServers = configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092";
    private readonly string _topic = configuration["KAFKA_TOPIC"] ?? "orders";
    private readonly string _groupId = configuration["KAFKA_GROUP_ID"] ?? "order-processor-group";
    private readonly Counter<long> _ordersProcessed = ordersProcessed;
    private readonly Histogram<double> _processingTime = processingTime;

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

        logger.LogInformation("Consumer started. Listening on topic {Topic}", _topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);

                var stopwatch = Stopwatch.StartNew();

                var order = JsonSerializer.Deserialize<OrderRequest>(
                    result.Message.Value,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (order is null) continue;

                logger.LogInformation("Processing order {OrderId}", order.OrderId);

                stopwatch.Stop();

                _ordersProcessed.Add(1);
                _processingTime.Record(stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
            {
                logger.LogWarning("Topic '{Topic}' not available yet. Retrying in 2s...", _topic);
                await Task.Delay(2000, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error consuming message");
            }
        }

        consumer.Close();
    }
}