using Grpc.Net.Client;
using TccMicroservices.Grpc;
using OrderRequestDomain = OrderGateway.Grpc.Models.OrderRequest;
using OrderRequestGrpc = TccMicroservices.Grpc.OrderRequest;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapPost("/api/orders", async (OrderRequestDomain request, IConfiguration configuration) =>
{
    var processorUrl = configuration["PROCESSOR_URL"]
        ?? throw new InvalidOperationException("PROCESSOR_URL not configured");

    using var channel = GrpcChannel.ForAddress(processorUrl);
    var client = new OrderService.OrderServiceClient(channel);

    var grpcRequest = new OrderRequestGrpc
    {
        OrderId = request.OrderId.ToString(),
        ProductId = request.ProductId,
        Quantity = request.Quantity,
        UnitPrice = (double)request.UnitPrice,
        CustomerId = request.CustomerId,
        CreatedAt = request.CreatedAt.ToString("O")
    };

    var response = await client.ProcessOrderAsync(grpcRequest);

    return Results.Ok(new
    {
        orderId = response.OrderId,
        status = response.Status,
        processedAt = response.ProcessedAt
    });
});

app.Run();