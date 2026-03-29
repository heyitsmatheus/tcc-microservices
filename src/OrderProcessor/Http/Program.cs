using OrderProcessor.Models;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapPost("/api/orders", (OrderRequest request, ILogger<Program> logger) =>
{
    logger.LogInformation("Processing order {OrderId}", request.OrderId);

    var response = new OrderResponse
    {
        OrderId = request.OrderId,
        Status = "PROCESSED",
        ProcessedAt = DateTime.UtcNow
    };

    return Results.Ok(response);
});

app.Run();