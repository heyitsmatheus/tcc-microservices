using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using OrderProcessor.Kafka.Services;

var meter = new Meter("OrderProcessor.Kafka");
var ordersProcessed = meter.CreateCounter<long>(
    "orders_processed",
    description: "Total de pedidos processados");
var processingTime = meter.CreateHistogram<double>(
    "order_processing_time_ms",
    unit: "ms",
    description: "Tempo de processamento interno do pedido");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("OrderProcessor.Kafka")
            .AddPrometheusExporter();
    });

builder.Services.AddSingleton(ordersProcessed);
builder.Services.AddSingleton(processingTime);
builder.Services.AddHostedService<OrderConsumerService>();

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();

app.Run();