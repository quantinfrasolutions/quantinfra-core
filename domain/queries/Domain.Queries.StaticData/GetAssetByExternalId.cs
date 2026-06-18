using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.StaticData;
namespace QuantInfra.Domain.Queries.StaticData;

public record GetAssetByExternalId(int BrokerId, string ExternalId) : IQuery<Asset?>;