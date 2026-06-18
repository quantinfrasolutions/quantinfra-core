using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Common.Interfaces.Api.StaticData
{
    public class StreamListView
	{
        public StreamListView(int streamId, string? ticker, int datafeedId, string datafeedName, int? contractId, string? contractName, ConstantStreamValue? constantStreamValue)
        {
            StreamId = streamId;
            Ticker = ticker;
            DatafeedId = datafeedId;
            DatafeedName = datafeedName;
            ContractId = contractId;
            ContractName = contractName;
            ConstantStreamValue = constantStreamValue;
        }

        public int StreamId { get; init; }
        public string? Ticker { get; init; }
        public int DatafeedId { get; init; }
        public string DatafeedName { get; init; }
        public int? ContractId { get; init; }
        public string? ContractName { get; init; }
        public ConstantStreamValue? ConstantStreamValue { get; init; }
    }
}

