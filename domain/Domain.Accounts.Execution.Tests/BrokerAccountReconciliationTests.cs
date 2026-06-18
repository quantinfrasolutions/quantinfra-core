// using Common.Accounts.Abstractions;
// using Common.EventSourcing;
// using Domain.Accounts;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using QuantInfra.Common.Accounts.Abstractions;
// using QuantInfra.Common.Accounts.Abstractions.AccountStates;
// using QuantInfra.Common.StaticData.Abstractions;
// using QuantInfra.Tests.Mocks;
//
// namespace Tests.Unit.Domain.Accounts;
//
// public class BrokerAccountReconciliationTests
// {
//     private BrokerAccountState _state;
//
//     private InMemoryBus _bus;
//     private BrokerAccount _account;
// #pragma warning disable NUnit1032
//     private ILoggerFactory _loggerFactory = new LoggerFactory();
// #pragma warning restore NUnit1032
//     private MockInvestmentQueryHandler _investmentQueryHandler;
//     private MockGetExecutionRequestsForSignalGroupQueryHandler _ersQueryHandler;
//     private MockGetOrdersHistoryHandler _historyHandler;
//     private MockEventHandler _eventHandler;
//     private Dictionary<string, Guid> _ids;
//     private InMemoryStaticDataRepository _sdRepository;
//     private MockAccountsFactory _accountsFactory;
//     private MockBrokerAccount _brokerAccount;
//     private MockTradingClient _tradingClient;
//
//
//     [SetUp]
//     public void Setup()
//     {
//         _sdRepository = new();
//         _investmentQueryHandler = new();
//         _ersQueryHandler = new();
//         _historyHandler = new();
//         _eventHandler = new();
//         _ids = new();
//         _accountsFactory = new();
//         _tradingClient = new();
//         // _tradingClient = new();
//         
//         var sc = new ServiceCollection()
//             .BuildServiceProvider();
//         
//         _bus = new(sc);
//         
//         _state = BrokerAccountState.CreateNewState(new AccountRecord
//         {
//             AccountId = Guid.NewGuid(),
//             AccountType = AccountType.ExecutableSubAccount
//         });
//         
//         _account = new BrokerAccount(
//             _state,
//             1,
//             BrokerType.IBKR,
//             _bus,
//             _bus,
//             null,
//             _accountsFactory,
//             _tradingClient,
//             _sdRepository
//         );
//     }
//
//     public void TestReconciliation()
//     {
//         // Successful reconciliation
//         // Missing order
//         // Not required order
//         // Missing position
//         // Not required position
//         // Position size discrepancy
//     }
//     
//     private void SetupStaticData()
//     {
//         _sdRepository.CreateBroker(new Broker(1, "test broker", BrokerType.IBKR));
//         _sdRepository.CreateAsset(new Asset
//         {
//             Id = 840,
//             AssetType = AssetType.Currency
//         });
//         _sdRepository.CreateCurrency(new Currency
//         {
//             Id = 840
//         });
//         _sdRepository.CreateAsset(new Asset
//         {
//             Id = 10000
//         });
//         _sdRepository.CreateContractTemplate(new ContractTemplate
//         {
//             AssetId = 10000,
//             BrokerId = 1
//         });
//         _sdRepository.CreateContract(new ContractDefinition
//         {
//             ContractId = 10000,
//             TemplateId = 10000,
//             ExternalContractId = "test-id"
//         });
//     }
// }