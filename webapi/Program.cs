using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Transcoder.WebApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapPost("/videotranscode", async ([FromBody] TranscodeRequest request) =>
{
    var factory = new ConnectionFactory()
    {
        Uri = new Uri("amqp://user:password@rabbitmq:5672")
    };

    IConnection conn = await factory.CreateConnectionAsync();
    using var channel = await conn.CreateChannelAsync();

    string transcoderExchange = "transcoder";

    await channel.ExchangeDeclareAsync(exchange: transcoderExchange,
    type: ExchangeType.Fanout);

    var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));

    await channel.BasicPublishAsync(
        exchange: transcoderExchange,
        routingKey: string.Empty,
        body: body
    );

});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
