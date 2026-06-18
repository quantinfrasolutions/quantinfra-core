// using System.Text.Json;
// using Common.Infrastructure.Abstractions;
// using Common.Messaging;
// using Common.Messaging.Json;
// using Common.Messaging.ZeroMq.DealerRouterWithReplay;
// using Common.Utils;
// using Domain.Queries.Accounts.AccountsService;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using NodaTime;
// using QuantInfra.Common.Accounts.Abstractions;
// using QuantInfra.Common.Accounts.Abstractions.AccountStates;
// using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
//
// namespace StrategiesCore.Tests;
//
// [TestFixture]
// public class ApiTests
// {
//     private IServiceProvider _sp;
//     private StrategiesService _ss;
//     private IAccountsServiceApi _serviceApi;
//     private LoggingRouter _router;
//
//     [OneTimeSetUp]
//     public async Task OneTimeSetUp()
//     {
//         _sp = MockService.BuildService(false);
//         _ss = _sp.GetRequiredService<StrategiesService>();
//         _serviceApi = _sp.GetRequiredService<IAccountsServiceApi>();
//         
//         _router = new LoggingRouter(new() { Protocol = "tcp", Port = 7777, }, _sp.GetRequiredService<ILogger<Router>>());
//         _router.Start();
//         
//         await _ss.StartAsync(CancellationToken.None);
//         
//         await Task.Delay(100);
//         Assert.That(_router.Messages.Count, Is.EqualTo(1)); // SessionStart
//         _router.Messages.Clear();
//     }
//
//     [Test, Order(1)]
//     public async Task Test1RequestAccountState()
//     {
//         await _serviceApi.SubscribeToAccountState(100, false);
//         await Task.Delay(100);
//         
//         Assert.That(_router.Messages.Count, Is.EqualTo(1));
//         var msg = _router.Messages[0];
//         Assert.That(msg.MessageType, Is.EqualTo(MessageType.DataMessage));
//
//         var msgFactory = new JsonMessageFactory(
//             new MultipleAssembliesTypeResolver(new List<string>()
//             {
//                 "Domain.Queries.Accounts.AccountsService",
//             }),
//             _sp.GetRequiredService<JsonSerializerOptions>()
//         );
//         var parsed = msgFactory
//             .CreateReceivedMessage(msg.Payload, SystemClock.Instance.GetCurrentInstant())
//             .GetWrappedObject();
//         Assert.That(parsed, Is.TypeOf<GetAccountState>());
//         var typed = (GetAccountState) parsed;
//         Assert.That(typed.AccountId, Is.EqualTo(100));
//     }
//     
//     [OneTimeTearDown]
//     public async Task OneTimeTearDown()
//     {
//         await _ss.StopAsync(CancellationToken.None);
//         _router.Dispose();
//     }
// }
//
// class LoggingRouter : Router
// {
//     public List<DownstreamMessage> Messages { get; } = new();
//     
//     public LoggingRouter(RouterConfig config, ILogger<Router> logger) : base(config, logger)
//     {
//     }
//
//     protected override void HandleIncomingMessage(DownstreamMessage message) => Messages.Add(message);
// }