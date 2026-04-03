using Microsoft.AspNetCore.Server.Kestrel.Core;
using OpenTelemetry.Metrics;
using OrderProcessor.Grpc.Services;
using System.Diagnostics.Metrics;

var meter = new Meter("OrderProcessor.Grpc");
var ordersProcessed = meter.CreateCounter<long>(
    "orders_processed",
    description: "Total de pedidos processados");
var processingTime = meter.CreateHistogram<double>(
    "order_processing_time_ms",
    unit: "ms",
    description: "Tempo de processamento interno do pedido");

var builder = WebApplication.CreateBuilder(args);

// Habilita HTTP/1.1 na porta 8080 e HTTP/2 na porta 5011
// HTTP/2 → necessário para gRPC
// HTTP/1.1 → necessário para o Prometheus fazer scraping do /metrics
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5011, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });

    options.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });
});

builder.Services.AddGrpc();

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddMeter("OrderProcessor.Grpc")
            .AddPrometheusExporter();
    });

builder.Services.AddSingleton(ordersProcessed);
builder.Services.AddSingleton(processingTime);

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();
app.MapGrpcService<OrderService>();

app.Run();