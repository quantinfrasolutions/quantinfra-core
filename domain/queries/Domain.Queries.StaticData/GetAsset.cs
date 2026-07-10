using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.StaticData;
namespace QuantInfra.Domain.Queries.StaticData;

public record GetAsset(int AssetId) : IQuery<Asset?>;