using Disruptor.Dsl;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Common.ServiceBase.Handlers;
using QuantInfra.Common.Trading.Infrastructure;

namespace QuantInfra.Services.ExecutionCore;

public class ExecutionService : IHostedService
{
    private readonly Config _config;
    private readonly Disruptor<IncomingDisruptorMessage> _inputDisruptor;
    private readonly Disruptor<OutgoingDisruptorMessage> _outputDisruptor;
    private readonly ITradingAccountsRepositoryReadonly _accountRecordsRepository;
    private readonly HostedTradingClientsProvider _tradingClientsProvider;
    private readonly IAccountsServiceApiReadonly _accountsServiceApi;
    private readonly Sender _sender;
    private readonly IEnumerable<IIncomingTransport> _incomingTransports;
    private readonly ExecutionServiceState _state;
    private readonly ILogger<ExecutionService> _logger;

    public ExecutionService(
        Config config,
        Disruptor<IncomingDisruptorMessage> inputDisruptor,
        Disruptor<OutgoingDisruptorMessage> outputDisruptor,
        Bpl bpl,
        ITradingAccountsRepositoryReadonly accountRecordsRepository,
        HostedTradingClientsProvider tradingClientsProvider,
        IAccountsServiceApiReadonly accountsServiceApi,
        Parser parser,
        Sender sender,
        ILoggerFactory loggerFactory, 
        IEnumerable<IIncomingTransport> incomingTransports,
        ExecutionServiceState state
    )
    {
        _logger = loggerFactory.CreateLogger<ExecutionService>();
        _config = config;
        _inputDisruptor = inputDisruptor;
        _outputDisruptor = outputDisruptor;
        _accountRecordsRepository = accountRecordsRepository;
        _tradingClientsProvider = tradingClientsProvider;
        _accountsServiceApi = accountsServiceApi;
        _sender = sender;
        _incomingTransports = incomingTransports;
        _state = state;
        // _inputDisruptor.ConfigureDisruptor(
        //     new(parser, 1),
        //     new(bpl, _config.UseSingleThreadForInputDisruptor ? 1 : 2)
        // );
        if (config.Monolith) _inputDisruptor.HandleEventsWith(bpl);
        else _inputDisruptor.HandleEventsWith(parser).Then(bpl);
        _inputDisruptor.SetDefaultExceptionHandler(new FailFastExceptionHandler<IncomingDisruptorMessage>(_logger));
        _outputDisruptor.HandleEventsWith(_sender);
        _outputDisruptor.SetDefaultExceptionHandler(new FailFastExceptionHandler<OutgoingDisruptorMessage>(_logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting service");
        
        _inputDisruptor.Start();
        _outputDisruptor.Start();
        _logger.LogInformation("Disruptors started");
        
        await _accountsServiceApi.StartAsync(cancellationToken);
        _logger.LogInformation("AccountsServiceApi started");
        
        _sender.Start();
        _logger.LogInformation("Sender started");

        var accounts = await _accountRecordsRepository.GetTradingAccountsByExecutionServiceId(_config.ExecutionServiceName);
        foreach (var a in accounts)
        {
            _state.Accounts.Add(a.AccountId, a);
        }
        _logger.LogInformation($"Serving {accounts.Count} accounts");

        await Task.WhenAll(_incomingTransports.Select(t => t.StartAsync(cancellationToken)));
        await Task.Delay(1000, cancellationToken);
        _logger.LogInformation("All incoming transports started");
        
        var timeout = 10000;
        var tasks = accounts.Select(s => 
            Task.Run(() => _accountsServiceApi.SubscribeToBrokerAccountState(s.AccountId, s.AccountServiceName, true, timeout, false), cancellationToken)
        );
        await Task.WhenAll(tasks);
        _logger.LogInformation("Subscribed to account updates");
        
        await _tradingClientsProvider.StartAsync(
            accounts.Select(a =>
            {
                var config = a.TradingClientConfig!;
                if (_config.WritePerformanceMetrics) config.WritePerformanceMetrics = true;
                return config;
            }).ToList(), 
            cancellationToken
        );
        _logger.LogInformation("Trading clients started");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _inputDisruptor.Halt();
        _outputDisruptor.Halt();
        return Task.CompletedTask;
    }
}