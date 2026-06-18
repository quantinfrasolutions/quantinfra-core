// using System.Buffers;
// using Binance.Futures.USDM;
//
// namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.MarketData;
//
// internal class Parser(IReadOnlyDictionary<string, int> streamsMap) : IWorkHandler<IncomingDisruptorMessage>
// {
//     public void OnEvent(IncomingDisruptorMessage data) => OnEvent(streamsMap, data);
//     
//     public static void OnEvent(IReadOnlyDictionary<string, int> streamsMap, IncomingDisruptorMessage data)
//     {
//         try
//         {
//             ReadOnlySpan<byte> json = data.Span;
//             
//             var kind = BinanceMessageRouter.Classify(json, out var svc);
//             switch (kind)
//             {
//                 case BinanceMsgKind.ServiceAck:
//                     if (svc.Id is { } id) data.SetSubscriptionConfirmation(id); // _subscriptionsManager.ConfirmSubscription(id); // TODO: error
//                     return;
//                 case BinanceMsgKind.MarketData:
//                     if (Kline1mParser.TryParseKline1m(json, streamsMap, out var kline)) data.SetKline1m(kline); // ProcessKLine(kline, receivedAt, swReceivedAt, swProcessingStart);
//                     return;
//             }
//         }
//         finally
//         {
//             // Return the rented buffer
//             ArrayPool<byte>.Shared.Return(data.Buffer);
//             data.Clear();
//         }
//     }
// }