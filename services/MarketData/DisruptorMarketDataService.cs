// using System.Threading;
// using System.Threading.Tasks;
// using Common.Messaging.ZeroMq.DealerRouterWithReplay;
// using Disruptor.Dsl;
// using MarketData;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
// using QuanInfra.Common.ServiceBase;
// using QuanInfra.Common.ServiceBase.Handlers;
// using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
// using QuantInfra.Common.ServiceBase;
// using QuantInfra.Common.ServiceBase.Handlers;
//
// namespace QuantInfra.Services.MarketData;
//
// public class DisruptorMarketDataService : IHostedService
// {
//     private readonly Router _router;
//     private readonly Bpl _bpl;
//     private readonly ReceiverFilter _receiverFilter;
//     private readonly Disruptor<IncomingDisruptorMessage> _inputDisruptor;
//     private readonly Disruptor<OutgoingDisruptorMessage> _outputDisruptor;
//
//     public DisruptorMarketDataService(
//         Router router,
//         Parser parser,
//         Bpl bpl,
//         MulticastSender multicast,
//         ReceiverFilter receiverFilter,
//         Disruptor<IncomingDisruptorMessage> inputDisruptor,
//         Disruptor<OutgoingDisruptorMessage> outputDisruptor,
//         Persister persister,
//         ILogger<DisruptorMarketDataService> logger
//     )
//     {
//         _router = router;
//         _bpl = bpl;
//         _receiverFilter = receiverFilter;
//         _inputDisruptor = inputDisruptor;
//         _outputDisruptor = outputDisruptor;
//         
//         var processingGroup = new SingleThreadProcessingGroup<IncomingDisruptorMessage>(parser, bpl);
//
//         // _inputDisruptor.HandleEventsWith(processingGroup); 
//         _inputDisruptor.HandleEventsWith(parser).Then(bpl);
//         _inputDisruptor.SetDefaultExceptionHandler(new FailFastExceptionHandler<IncomingDisruptorMessage>(logger));
//         _outputDisruptor.HandleEventsWith(multicast).Then(persister);
//         _outputDisruptor.SetDefaultExceptionHandler(new FailFastExceptionHandler<OutgoingDisruptorMessage>(logger));
//     }
//     
//     public async Task StartAsync(CancellationToken cancellationToken)
//     {
//         _inputDisruptor.Start();
//         _outputDisruptor.Start();
//         _receiverFilter.InitializeState();
//         
//         await _bpl.StartAsync(cancellationToken);
//         _router.Start();
//     }
//
//     public Task StopAsync(CancellationToken cancellationToken)
//     {
//         _router.Dispose();
//         _inputDisruptor.Halt();
//         _outputDisruptor.Halt();
//         return Task.CompletedTask;
//     }
// }