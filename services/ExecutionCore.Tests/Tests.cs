using Common.EventSourcing;
using Common.Trading.Infrastructure;
using ExecutionCore.Queries;
using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.Accounts.Abstractions;
using QuantInfra.Tests.Mocks;

namespace ExecutionCore.Tests;

[TestOf(typeof(ExecutionService))]
public class Tests
{
    private IServiceProvider _sp;
    private ExecutionService _es;
    
    [SetUp]
    public void Setup()
    {
        _sp = MockService.BuildService(false);
        _es = _sp.GetRequiredService<ExecutionService>();
    }

    [Test]
    public async Task TestStartExecutionService()
    {
        var repository = _sp.GetRequiredService<MockTradingAccountsRepositoryReadonly>();
        repository.Accounts.Add(new TradingClientConfig
        {
            AccountId = 100,
            ExecutionServiceName = "ES1",
            TradingClientClassName = "QuantInfra.Tests.Mocks.MockTradingClient",
            TradingClientParamsSerialized = "null"
        });
        
        var task = _es.StartAsync(CancellationToken.None);

        var queryBus = _sp.GetRequiredService<IQueryBus>();
        var delayTask = Task.Delay(1000);
        IHostedTradingClient? tc = null;
        var res = await Task.WhenAny(
            delayTask,
            Task.Run(() =>
            {
                do
                {
                    tc = queryBus.Query<GetTradingClient, IHostedTradingClient?>(new(100));
                    if (tc is not null) break;
                } while (true);
            })
        );
        Assert.That(res, Is.Not.EqualTo(delayTask));
        Assert.That(tc, Is.Not.Null);
        Assert.That(tc, Is.InstanceOf<MockTradingClient>());
        var mock = (MockTradingClient)tc;
        Assert.That(mock.RequestAccountFullSnapshotCalled, Is.True);

        var mockApi = _sp.GetRequiredService<MockAccountsServiceApi>();
        Assert.That(mockApi.BrokerAccountStateSubscriptions.Count, Is.EqualTo(1));
    }
}