using Confluent.Kafka;
using OpenTelemetry.Metrics;
using OrderGateway.Kafka.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddPrometheusExporter();
    });

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();

app.MapPost("/api/orders", async (OrderRequest request, IConfiguration configuration) =>
{
    var bootstrapServers = configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092";
    var topic = configuration["KAFKA_TOPIC"] ?? "orders";

    var config = new ProducerConfig { BootstrapServers = bootstrapServers };

    using var producer = new ProducerBuilder<string, string>(config).Build();

    var message = new Message<string, string>
    {
        Key = request.OrderId.ToString(),
        Value = JsonSerializer.Serialize(request)
    };

    await producer.ProduceAsync(topic, message);

    var response = new OrderResponse
    {
        OrderId = request.OrderId,
        Status = "ACCEPTED",
        ProcessedAt = DateTime.UtcNow
    };

    return Results.Accepted("/api/orders", response);
});

app.Run();