using System.Text.Json.Serialization;
using HotelStay.Api.Endpoints;
using HotelStay.Api.Providers;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hotel Stay API",
        Version = "v1",
        Description = "SkyRoute Hotel Availability API"
    });
});

// Serialize enums as strings (e.g. "Standard", not 0) to match spec.md and the frontend's types
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Register all hotel providers via DI — add a new provider here to extend
builder.Services.AddSingleton<IHotelProvider, PremierStaysProvider>();
builder.Services.AddSingleton<IHotelProvider, BudgetNestsProvider>();

// In-memory reservation store (no persistence by design)
builder.Services.AddSingleton<ReservationStore>();

// CORS for local React dev (port 5173 = Vite default)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "Hotel Stay API v1"
        );

        options.RoutePrefix = string.Empty;
    });
}


app.UseCors();


app.MapHotelEndpoints();


app.Run();


// Expose for integration tests
public partial class Program { }
