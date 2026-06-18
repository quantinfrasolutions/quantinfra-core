using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;
using ExecutableAppBase;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Extensions.Logging;
using NodaTime.Serialization.SystemTextJson;
using Prometheus;
using Prometheus.SystemMetrics;
using QuantInfra.Common.Messaging.Json;

namespace QuantInfra.Common.Utils.ExecutableAppBase;

public class AppBase
{
    WebApplicationBuilder _builder;    
    IConfiguration _runConfig;

    bool _useCors, _useHealthChecks, _useSwagger, _useMetrics, _useOpenApi;
    private int _healthChecksIntervalSeconds = 30;
    Action<MetricServerMiddleware.Settings> configureMetrics;

    public AppBase(string[] args)
    {
        _runConfig = new ConfigurationBuilder()
            .AddCommandLine(args)
            .AddEnvironmentVariables()
            .Build();

        _builder = WebApplication.CreateBuilder(args);
    }

    public AppBase UseJsonFileConfiguration()
    {
        if (_runConfig.GetChildren().Select(i => i.Key).Contains("file"))
        {
            var filePath = Path.Join(Directory.GetCurrentDirectory(), _runConfig.GetValue("file", "appsettings.json"));
            Console.WriteLine($"Using configuration file: {filePath}");
            _builder.Configuration.AddJsonFile(filePath, optional: false);
        }

        return this;
    }

    public AppBase UseEnvironmentVariables()
    {
        if (_runConfig.GetChildren().Where(i => i.Key == "use-env").Any())
        {
            _builder.Configuration.AddEnvironmentVariables();
        }        
        return this;
    }

    public AppBase AddLogging(Action<ILoggingBuilder, IConfiguration>? configure = null)
    {
        _builder.Services.AddLogging(c =>
        {
            c.ClearProviders();
            c.AddNLog();
            configure?.Invoke(c, _builder.Configuration);

            LogManager.Configuration = new NLogLoggingConfiguration(_builder.Configuration.GetSection("nlog"));            
        });

        return this;
    }

    public AppBase ConfigureControllers(
        Action<Microsoft.AspNetCore.Mvc.MvcOptions>? configure = null,
        params Assembly[] controllerAssemblies
    )
    {
        var builder = _builder.Services
            .AddControllers(options =>
            {
                options.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>();
                configure?.Invoke(options);
            })
            .AddControllersAsServices();

        if (controllerAssemblies?.Count() > 0)
        {
            foreach (var assembly in controllerAssemblies)
            {
                builder.AddApplicationPart(assembly);
            }
        }
        
        builder.AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
            options.JsonSerializerOptions.WriteIndented = true;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb);
            options.JsonSerializerOptions.Converters.Add(new JsonReadOnlyDictionaryConverter());
            options.JsonSerializerOptions.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;
        });
        
        _builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
            options.SerializerOptions.WriteIndented = true;
            options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.SerializerOptions.ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb);
            options.SerializerOptions.Converters.Add(new JsonReadOnlyDictionaryConverter());
            options.SerializerOptions.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;
        });        
        return this;
    }

    public AppBase AddJsonOptions()
    {
        _builder.Services.AddSingleton(sp => sp.GetService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()?.Value.SerializerOptions);
        return this;
    }

    // public AppBase AddEndpointsApiExplorer()
    // {
    //     _builder.Services.AddEndpointsApiExplorer();
    //     return this;
    // }
    
    // public AppBase AddSwaggerDocument(string title)
    // {
    //     _useSwagger = true;
    //     _builder.Services.AddSwaggerDocument(o =>
    //     {
    //         o.Title = title;
    //     });
    //     return this;
    // }

    public AppBase AddOpenApiDocumentGenerator(string? documentName = null)
    {
        _useOpenApi = true;
        if (string.IsNullOrEmpty(documentName)) _builder.Services.AddOpenApi();
        else _builder.Services.AddOpenApi(documentName);
        return this;
    }

    public AppBase AddCors()
    {
        _useCors = true;
        _builder.Services.AddCors(options =>
        {
            options
                .AddPolicy(
                    name: "AllowLocalhost",
                    policy =>
                    {
                        policy
                            .AllowAnyOrigin()
                            .SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    }
                );
        });
        return this;
    }

    public AppBase AddHealthChecks(Action<IHealthChecksBuilder> configure)
    {
        configure(_builder.Services.AddHealthChecks());
        _useHealthChecks = true;
        return this;
    }

    public AppBase ConfigureServices(Action<WebApplicationBuilder, IServiceCollection, IConfiguration> configure)
    {
        configure(_builder, _builder.Services, _builder.Configuration);

        return this;
    }

    public AppBase AddMetrics(Action<MetricServerMiddleware.Settings>? configure = null)
    {
        _useMetrics = true;
        _builder.Services.AddSystemMetrics();
        configureMetrics = metrics =>
        {
            configure?.Invoke(metrics);
        };
        return this;
    }

    public AppBase WriteHealthReportToMetrics(string sectionName = "health-checks.publisher")
    {
        _builder.Services.Configure<HealthCheckPublisherOptions>(options =>
        {
            var splitSections = sectionName.Split('.');
            IConfigurationSection section = _builder.Configuration.GetSection(splitSections[0]);
            var i = 1;
            while (i < splitSections.Length)
            {
                section = section.GetSection(splitSections[i]);
                i++;
            }
            section.Bind(options);
        });
        
        _builder.Services.AddSingleton<IHealthCheckPublisher, HealthCheckPublisher>();

        return this;
    }

    public WebApplication Build()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        
        var app = _builder.Build();

        // Configure the HTTP request pipeline.
        if (_useCors)
        {
            app.UseCors("AllowLocalhost");
        }
        if (app.Environment.IsDevelopment() && _useOpenApi)
        {
            app.MapOpenApi().CacheOutput();
        }
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        if (_useHealthChecks)
        {
            app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                ResponseWriter = HealthCheckResponseWriter.WriteResponse
            });
            app.MapHealthChecks("/healthz/liveness", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                ResponseWriter = HealthCheckResponseWriter.WriteResponseForKubernetes
            });
        }
        if (_useMetrics)
        {
            app.MapMetrics(configureMetrics, "/api/metrics");
        }
        app.UseStaticFiles();

        return app;
    }
}

