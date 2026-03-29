using OrderProcessor.Kafka.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<OrderConsumerService>();

var app = builder.Build();

app.Run();