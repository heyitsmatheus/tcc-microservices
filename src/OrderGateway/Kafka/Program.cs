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

// 🔥 Registrar Producer como Singleton
builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();

    var config = new ProducerConfig
    {
        BootstrapServers = configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092"
    };

    return new ProducerBuilder<string, string>(config).Build();
});

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();

app.MapPost("/api/orders", async (
    OrderRequest request,
    IConfiguration configuration,
    IProducer<string, string> producer) =>
{
    var topic = configuration["KAFKA_TOPIC"] ?? "orders";

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