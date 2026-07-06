using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuantInfra.Api;
using QuantInfra.Api.Client;
using QuantInfra.Common.Interfaces.Api;
using Radzen;
using UI.Interfaces;
using UI.Interfaces.Accounts;
using UI.Interfaces.Infrastructure;
using UI.Interfaces.StaticData;
using UI.Interfaces.Strategies;

namespace UI.ApiWrapper;

public sealed partial class ApiRepository
{
    private readonly ServiceWrapper _wrapper;
    private NotificationService _notificationService;
    private ILogger _logger;
    
    public ApiRepository(ServiceWrapper wrapper, NotificationService notificationService, ILoggerFactory loggerFactory)
    {
        _wrapper = wrapper;
        _notificationService = notificationService;
        _logger = loggerFactory.CreateLogger<ApiRepository>();
    }


    #region Helpers

    internal async Task Call(
         string successMessage,
         string errorMessage,
         Func<Task> func
     )
     {
         try
         {
             await func();
             _notificationService.Notify(NotificationSeverity.Success, successMessage);
         }
         catch (SwaggerException<ValidationProblemDetails> ex)
         {
             _notificationService.Notify(NotificationSeverity.Error, errorMessage);
             throw new ValidationException(ex.Result);
         }
         catch (Exception ex)
         {
             _notificationService.Notify(NotificationSeverity.Error, ex.Message);
             _logger.LogError(ex, errorMessage);
             _logger.LogError(ex.InnerException, "Inner exception");
             throw;
         }
     }

    /// <summary>
    /// Basic retrieve
    /// </summary>
    internal async Task<TApiResult> Retrieve<TFilter, TApiResult>(
        string name,
        TFilter? filter,
        Func<TFilter?, Task<TApiResult>> apiMethod
    )
    {
        try
        {
            return await apiMethod(filter);
        }
        catch (Exception ex)
        {
            _notificationService.Notify(NotificationSeverity.Error, ex.Message);
            _logger.LogError(ex, $"Error retrieving {name}");
            throw;
        }
    }
    
    internal async Task<TApiResult> Retrieve<TApiResult>(
        string name,
        Func<Task<TApiResult>> apiMethod
    )
    {
        try
        {
            return await apiMethod();
        }
        catch (Exception ex)
        {
            _notificationService.Notify(NotificationSeverity.Error, ex.Message);
            _logger.LogError(ex, $"Error retrieving {name}");
            throw;
        }
    }
    
    /// <summary>
    /// Retrieve collection
    /// </summary>
    internal Task<IEnumerable<TApiResult>> RetrieveCollection<TApiResult>(
        string name,
        Func<Task<IEnumerable<TApiResult>>> apiMethod
    ) => Retrieve(name, apiMethod);

    #endregion
}

public static class Extensions
{
    public static IServiceCollection AddApiRepository(this IServiceCollection services) => services
        .AddScoped<ApiRepository>();
    
    public static IServiceCollection UseApiStaticDataRepository(this IServiceCollection sc) => sc
        .AddScoped<IUiStaticDataRepository>(sp => sp.GetRequiredService<ApiRepository>());
    
    public static IServiceCollection UseApiAccountsRepository(this IServiceCollection sc) => sc
        .AddScoped<IUiAccountsRepository>(sp => sp.GetRequiredService<ApiRepository>());
    
    public static IServiceCollection UseApiStrategiesRepository(this IServiceCollection sc) => sc
        .AddScoped<IUiStrategiesRepository>(sp => sp.GetRequiredService<ApiRepository>())
        .AddScoped<IUiStrategyClassesRepository>(sp => sp.GetRequiredService<ApiRepository>());
        
    public static IServiceCollection UseApiInfrastructureRepository(this IServiceCollection sc) => sc
        .AddScoped<IUiInfrastructureRepository>(sp => sp.GetRequiredService<ApiRepository>());
}