using Common.Utils.Reflection;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Trading.Infrastructure;

namespace QuantInfra.Common.Trading.Infrastructure;

public class TradingClientFactory : ITradingClientFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITypeResolver _typeResolver;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IClock _clock;
    private readonly ITradingClientResponsesHandler _handler;

    public TradingClientFactory(IServiceProvider serviceProvider, ITypeResolver typeResolver, ILoggerFactory loggerFactory, IClock clock,
        ITradingClientResponsesHandler handler)
    {
        _serviceProvider = serviceProvider;
        _typeResolver = typeResolver;
        _loggerFactory = loggerFactory;
        _clock = clock;
        _handler = handler;
    }

    public IHostedTradingClient GetTradingClient(TradingClientConfig config)
    {
        var type = _typeResolver.ResolveType(config.TradingClientClassName);
        // var config = clientParams.ToConfig(configType);
        return (IHostedTradingClient)Activator.CreateInstance(
            type,
            config,
            _serviceProvider,
            _loggerFactory,
            _handler,
            _clock
        );
    }
}