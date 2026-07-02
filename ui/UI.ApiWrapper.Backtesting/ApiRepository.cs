using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuantInfra.Api.Client.Backtesting;
using QuantInfra.Common.Backtesting.Abstractions;
using QuantInfra.UI.Interfaces.Backtesting;
using Radzen;

namespace QuantInfra.UI.ApiWrapper.Backtesting;

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
    public static IServiceCollection AddBacktestingApiRepository(this IServiceCollection sc) => sc
        .AddScoped<ApiRepository>()
        .AddScoped<IUiTestUnitsRepository>(sp => sp.GetRequiredService<ApiRepository>())
        .AddScoped<IUiBacktestResultsRepository>(sp => sp.GetRequiredService<ApiRepository>())
        .AddScoped<ITestServer>(sp => sp.GetRequiredService<ApiRepository>())
        ;
}