using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using OrderProcessor.Models;

var meter = new Meter("OrderProcessor.Http");
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
            .AddAspNetCoreInstrumentation()  // latência HTTP automática
            .AddMeter("OrderProcessor.Http") // métricas customizadas
            .AddPrometheusExporter();        // expõe /metrics
    });

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();

app.MapPost("/api/orders", (OrderRequest request, ILogger<Program> logger) =>
{
    var stopwatch = Stopwatch.StartNew();

    logger.LogInformation("Processing order {OrderId}", request.OrderId);

    var response = new OrderResponse
    {
        OrderId = request.OrderId,
        Status = "PROCESSED",
        ProcessedAt = DateTime.UtcNow
    };

    stopwatch.Stop();

    ordersProcessed.Add(1);
    processingTime.Record(stopwatch.Elapsed.TotalMilliseconds);

    return Results.Ok(response);
});

app.Run();