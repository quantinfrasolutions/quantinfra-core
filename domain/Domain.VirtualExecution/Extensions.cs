using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.VirtualExecution.EventHandlers;

namespace QuantInfra.Domain.VirtualExecution;

public static class Extensions
{
    // public static IServiceCollection UseVirtualExecutor(this IServiceCollection sc) => sc
    //     .AddSingleton<VirtualExecutor>()
    //     .AddScoped<ForwardsOrderToAndFromVirtualExecutor>()
    //     .AddScoped<IEventHandler<ExecutionReportEvt>>(sp => sp.GetService<ForwardsOrderToAndFromVirtualExecutor>()!)
    //     .AddScoped<IEventHandler<VirtualOrderStatusChangedEvt>>(sp => sp.GetService<ForwardsOrderToAndFromVirtualExecutor>()!);
    
    public static IServiceCollection UseVirtualExecutorWithSingletonHandlers(this IServiceCollection sc) => sc
        .AddSingleton<VirtualExecutor>()
        .AddSingleton<ForwardOrdersToVirtualExecutor>()
        .AddSingleton<IEventHandler<ExecutionReportEvt>>(sp => sp.GetService<ForwardOrdersToVirtualExecutor>()!)
        .AddSingleton<IConfigurableLoggingModule>(sp => sp.GetService<ForwardOrdersToVirtualExecutor>()!)
        .AddSingleton<IConfigurableLoggingModule>(sp => sp.GetRequiredService<VirtualExecutor>());
}