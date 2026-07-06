using System.Globalization;
using System.Xml.Serialization;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using QuantInfra.Core.Services.Api.StaticData;
using QuantInfra.Services.Api;

namespace OpenApiGenerator;

public class Program
{
    public static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            })
            .AddApplicationPart(typeof(AccountsController).Assembly)
            .AddApplicationPart(typeof(StaticDataController).Assembly)
            .AddControllersAsServices();
        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi(options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
            options.AddSchemaTransformer((schema, context, cancellationToken) =>
            {
                if (context.JsonTypeInfo.Type == typeof(Instant) || context.JsonTypeInfo.Type == typeof(Instant?))
                {
                    schema.Type = "string";
                    schema.Format = "date-time";
                }

                return Task.CompletedTask;
            });
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                if (operation.Parameters == null) return Task.CompletedTask;
                foreach (var p in operation.Parameters)
                {
                    if (p == null) continue;
                    if (p.Name == "FromDt" || p.Name == "ToDt" || p.Name == "CloseDtFrom" || p.Name == "CloseDtTo"
                        || p.Name == "HistoryOpenDtFrom" || p.Name == "HistoryOpenDtTo"
                        || p.Name == "OpenDtFrom" || p.Name == "OpenDtTo" || p.Name == "Dt"
                    )
                    {
                        p.Schema = new OpenApiSchema()
                        {
                            Type = "integer",
                            Format = "int64",
                        };
                    }

                    if (p.Name == "ExternalId" || p.Name == "Name" || p.Name == "Ticker")
                    {
                        p.Schema = new OpenApiSchema()
                        {
                            Type = "string",
                        };
                    }
                }
                
                return Task.CompletedTask;
            });
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