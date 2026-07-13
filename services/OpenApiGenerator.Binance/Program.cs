using Microsoft.OpenApi;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using QuantInfra.Services.Api.Binance;

namespace OpenApiGenerator.Binance;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            })
            .AddApplicationPart(typeof(BinanceController).Assembly)
            .AddControllersAsServices();
        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi(options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
        });
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.NumberHandling =
                System.Text.Json.Serialization.JsonNumberHandling.Strict;
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.Run();
    }
}