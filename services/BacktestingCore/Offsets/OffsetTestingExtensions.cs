// using System;
// using System.Collections.Generic;
// using QuantInfra.Sdk.Strategies;
//
// namespace QuantInfra.Services.BacktestingCore.Offsets;
//
// public static class OffsetTestingExtensions
// {
//     public static IEnumerable<StrategyConfig> GetOffsetConfigs(this IEnumerable<StrategyConfig> strategies, OffsetTestingOptions options) =>
//         throw new NotImplementedException();
//         // Enumerable
//         //     .Range(options.MinOffset, options.MaxOffset + 1)
//         //     .Where(x => x % options.OffsetStepMinutes == 0)
//         //     .SelectMany(x =>
//         //     {
//         //         return strategies.Select(sc =>
//         //         {
//         //             var barStorages = new Dictionary<string, BarStorageConfig>(sc.RequiredBarStorages)
//         //             {
//         //                 ["main"] = new(sc.RequiredBarStorages["main"]) { Offset = Period.FromMinutes(x) }
//         //             };
//         //             return new StrategyConfig(sc)
//         //             {
//         //                 StrategyId = x == 0 ? sc.StrategyId : Guid.NewGuid(),
//         //                 RequiredBarStorages = barStorages
//         //             };
//         //         });
//         //     });
// }