// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Common.MarketData.Synthetics;
// using NodaTime;
// using QuantInfra.Sdk.MarketData;
// using QuantInfra.Sdk.StaticData.Synthetics;
//
// namespace QuantInfra.Common.MarketData.Synthetics;
//
// /// <summary>
// /// Singleton service that calculates synthetic contracts
// /// </summary>
// public class InMemorySyntheticsWatcher
// {
//     private Dictionary<int, SyntheticContractBuffer> _buffers = new(); 
//     private Instant _nextSyntheticRollover = Instant.MaxValue;
//     private readonly Dictionary<int, Instant> _syntheticContractsNextRolloverDts = new();
//     private Dictionary<int, List<SyntheticContractBuffer>> _syntheticBuffersByStream = new();
//
//     public void AddSyntheticContract(int contractId, Instant dt/*, IStaticDataProvider sdProvider*/)
//     {
//         throw new NotImplementedException();
//         // if (!_buffers.ContainsKey(contractId))
//         // {
//         //     var contract = sdProvider.GetContract(contractId);
//         //     
//         //     var buffer = new SyntheticContractBuffer(
//         //         contractId,
//         //         contract.StreamId, 
//         //         contract.SyntheticContractType switch
//         //         {
//         //             SyntheticContractType.Index => new IndexPriceCalculator(contract),
//         //             SyntheticContractType.Multiplicative => new MultiplicativePriceCalculator(contract),
//         //             SyntheticContractType.Rolling => new RollingPriceCalculator(contract),
//         //             _ => throw new ArgumentException($"Adding non-synthetic contract {contractId} as synthetic")
//         //         },
//         //         contract.GetCurrentSyntheticContractComposition(dt)
//         //     );
//         //
//         //     var nextComposition = contract.GetNextSyntheticContractComposition(dt);
//         //
//         //     if (nextComposition != null)
//         //     {
//         //         var nextRolloverDt = nextComposition.ValidFrom!.Value;
//         //         _syntheticContractsNextRolloverDts[contractId] = nextRolloverDt;
//         //         _nextSyntheticRollover = Instant.Min(_nextSyntheticRollover, nextRolloverDt);
//         //     }
//         //     
//         //     _buffers.Add(contractId, buffer);
//         //     InitializeByStreamMapping(sdProvider);
//         // }
//     }
//
//     private void InitializeByStreamMapping(/*IStaticDataProvider sdProvider*/)
//     {
//         throw new NotImplementedException();
//         // _syntheticBuffersByStream = _buffers
//         //     .SelectMany(b => b.Value.Weights
//         //         .Select(w => new { Buffer = b.Value, ContractId = w.Key })
//         //     )
//         //     .GroupBy(i => i.ContractId)
//         //     .ToDictionary(
//         //         gr => sdProvider.GetContract(gr.Key).StreamId!.Value,
//         //         gr => gr.Select(x => x.Buffer).ToList()
//         //     );
//     }
//
//     public Dictionary<int, CompositionUpdate>? CheckCompositionUpdate(int contractId, Instant ts/*, IStaticDataProvider sdProvider*/)
//     {
//         if (ts < _nextSyntheticRollover) return null;
//
//         var rolled = _syntheticContractsNextRolloverDts
//             .Where(kv => kv.Value <= ts)
//             .Select(kv => kv.Key)
//             .ToArray();
//
//         var res = rolled.ToDictionary(
//             cid => cid,
//             cid =>
//             {
//                 var contract = sdProvider.GetContract(cid);
//                 
//                 var nextComposition = contract
//                     .SyntheticContractCompositionHistory!
//                     .Where(h => h!.ValidFrom >= _nextSyntheticRollover && h!.ValidFrom <= ts)   // Just in case we missed one of the updates
//                     .OrderByDescending(h => h!.ValidFrom)                                       // take the latest valid before the current ts
//                     .First(); // if we got here
//
//                 return new CompositionUpdate(_buffers[cid].Composition, nextComposition!, contract.SynthRequiresBarRecalculationAtRollover!.Value,
//                     _buffers[cid].LastPrice);
//             });
//         
//         return res;
//     }
//     
//     public List<ExchangeBar> OnExchangeBar(ExchangeBar bar, int contractId)
//     {
//         if (_syntheticBuffersByStream.TryGetValue(bar.StreamId, out var synthetics))
//         {
//             return synthetics
//                 .Select(s => s.AppendExchangeBar(bar, contractId))
//                 .Where(x => x != null)
//                 .ToList()!;
//         }
//
//         return new(0);
//     }
//
//     public List<ExchangeTrade> OnExchangeTick(ExchangeTrade tick)
//     {
//         throw new NotImplementedException();
//
//         if (tick.Dt > _nextSyntheticRollover)
//         {
//             
//         }
//     }
// }