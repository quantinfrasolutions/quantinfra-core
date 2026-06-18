// using Common.Messaging;
// using Common.Metrics;
// using QuantInfra.Common.ServiceBase;
// using Disruptor.Dsl;
// using Microsoft.Extensions.Hosting;
// using QuanInfra.Common.ServiceBase;
// using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
//
// namespace Binance.Futures.USDM;
//
// public class DisruptorPublisher : IPublisherFactory, IPublisher
// {
//     private readonly Disruptor<OutgoingDisruptorMessage> _disruptor;
//
//     public DisruptorPublisher(Disruptor<OutgoingDisruptorMessage> disruptor)
//     {
//         _disruptor = disruptor;
//     }
//     
//     public IPublisher GetPublisher(string name) => this;
//     
//
//     public void PublishUnwrappedObject(object o)
//     {
//         _disruptor.PublishMessage(o);
//     }
//
//     public void PublishUnwrappedObjectWithReceiptionSwMicro(object o, long swReceivedAt)
//     {
//         _disruptor.PublishMessage(o, swPublishedAt: MetricsUtils.GetUnixMicro(), swReceivedAt: swReceivedAt);
//     }
//
//     public void Dispose()
//     {
//         
//     }
//
//     public void PublishUnwrappedString(Type type, string typeName, string data)
//     {
//         throw new NotImplementedException();
//     }
//
//     public void PublishWrappedMessage(IMessage message)
//     {
//         throw new NotImplementedException();
//     }
// }
//
// public class DisruptorService : IHostedService
// {
//     private readonly Disruptor<OutgoingDisruptorMessage> _disruptor;
//     private readonly Sender _sender;
//
//     public DisruptorService(
//         Disruptor<OutgoingDisruptorMessage> disruptor,
//         Sender sender
//     )
//     {
//         _disruptor = disruptor;
//         _sender = sender;
//         disruptor.HandleEventsWith(sender);
//     }
//
//     public Task StartAsync(CancellationToken cancellationToken)
//     {
//         _disruptor.Start();
//         _sender.Start();
//         return Task.CompletedTask;
//     }
//
//     public Task StopAsync(CancellationToken cancellationToken)
//     {
//         _disruptor.Halt();
//         return Task.CompletedTask;
//     }
// }