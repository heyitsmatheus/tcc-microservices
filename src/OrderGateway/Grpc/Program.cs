using Grpc.Net.Client;
using OpenTelemetry.Metrics;
using TccMicroservices.Grpc;
using OrderRequestDomain = OrderGateway.Grpc.Models.OrderRequest;
using OrderRequestGrpc = TccMicroservices.Grpc.OrderRequest;

// Permite HTTP/2 sem TLS (necessário para gRPC over plaintext)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

// Registra o canal gRPC como Singleton
// O canal gerencia o pool de conexões HTTP/2 internamente
// Equivalente ao IHttpClientFactory no REST
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var processorUrl = configuration["PROCESSOR_URL"]
        ?? throw new InvalidOperationException("PROCESSOR_URL not configured");

    return GrpcChannel.ForAddress(processorUrl);
});

// Registra o client como Singleton — thread-safe por design
builder.Services.AddSingleton(sp =>
{
    var channel = sp.GetRequiredService<GrpcChannel>();
    return new OrderService.OrderServiceClient(channel);
});

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddPrometheusExporter();
    });

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();

app.MapPost("/api/orders", async (
    OrderRequestDomain request,
    OrderService.OrderServiceClient client) =>
{
    var grpcRequest = new OrderRequestGrpc
    {
        OrderId = request.OrderId.ToString(),
        ProductId = request.ProductId,
        Quantity = request.Quantity,
        UnitPrice = (double)request.UnitPrice,
        CustomerId = request.CustomerId,
        CreatedAt = request.CreatedAt.ToString("o")
    };

    var response = await client.ProcessOrderAsync(grpcRequest);

    return Results.Ok(new OrderResponse
    {
        OrderId = response.OrderId,
        Status = response.Status,
        ProcessedAt = response.ProcessedAt
    });
});

app.Run();