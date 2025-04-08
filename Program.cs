using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Mapeia o endpoint de health check em /health
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var options = new JsonWriterOptions { Indented = true };

        context.Response.ContentType = MediaTypeNames.Application.Json;

        await using var stream = context.Response.BodyWriter.AsStream();
        await using var writer = new Utf8JsonWriter(stream, options);
        writer.WriteStartObject();
        writer.WriteString("status", report.Status.ToString());
        writer.WriteStartObject("results");

        foreach (var entry in report.Entries)
        {
            writer.WriteStartObject(entry.Key);
            writer.WriteString("status", entry.Value.Status.ToString());
            writer.WriteString("description", entry.Value.Description);
            writer.WriteStartObject("data");
            foreach (var item in entry.Value.Data)
            {
                writer.WritePropertyName(item.Key);
                JsonSerializer.Serialize(writer, item.Value, item.Value?.GetType() ?? typeof(object));
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
        await writer.FlushAsync();
    }
});


var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
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

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
