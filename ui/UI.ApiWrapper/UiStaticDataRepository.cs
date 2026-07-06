using QuantInfra.Common.Interfaces.Api;
using QuantInfra.Common.Interfaces.Api.StaticData;
using QuantInfra.Sdk.StaticData;
using UI.Interfaces.StaticData;
using Stream = QuantInfra.Sdk.StaticData.Stream;

namespace UI.ApiWrapper;

public partial class ApiRepository : IUiStaticDataRepository
{
    #region Assets
    
    public Task<IEnumerable<AssetView>> GetAssets(AssetFilter? filter = null) =>
        RetrieveCollection("assets", () => _wrapper.Client.GetAssetsAsync(filter?.Id, filter?.Name, filter?.AssetType?.ToString(), filter?.Limit, filter?.Offset));

    public Task CreateAsset(CreateAssetRequest asset) =>
        Call("Asset created", "Failed to create asset", () => _wrapper.Client.CreateAssetAsync(asset));

    public Task DeleteAsset(long id)
    {
        throw new NotImplementedException();
    }
    #endregion
    
    #region Brokers
    
    public Task<IEnumerable<Broker>> GetBrokers(BrokersFilter? filter = null) =>
        RetrieveCollection("brokers", () => _wrapper.Client.GetBrokersAsync(filter?.Limit, filter?.Offset));
    
    #endregion
    
    #region Commissions
    
    public Task<IEnumerable<CommissionStructure>> GetCommissions(CommissionsFilter filter) =>
        RetrieveCollection("commissions", () => _wrapper.Client.GetCommissionStructuresAsync(filter.CommissionId,
            filter.Name, filter.CurrencyId, filter.Type?.ToString(), filter.BrokerId, filter.ExchangeId, filter.Limit, filter.Offset));

    public Task CreateCommission(CreateCommissionRequest request) =>
        Call("Commission created", "Failed to create commission", () => _wrapper.Client.CreateCommissionStructureAsync(request));

    public Task DeleteCommission(int id)
    {
        throw new NotImplementedException();
    }

    public Task UpdateContractCommissions(int contractId, IEnumerable<int> add, IEnumerable<int> remove)
    {
        throw new NotImplementedException();
    }

    public Task UpdateCommission(CommissionStructure cs)
    {
        throw new NotImplementedException();
    }
    
    #endregion
    
    #region Contracts
    
    public Task<IEnumerable<ContractListView>> GetContracts(ContractsFilter? filter = null) =>
        RetrieveCollection("contracts",
            () => _wrapper.Client.GetContractsAsync(filter?.Ticker, filter?.ExchangeId, filter?.ContractIds,
                filter?.CommissionId, filter?.Limit, filter?.Offset));

    public Task CreateContract(CreateContractRequest request) =>
        Call("Contract created", "Failed to create contract", () => _wrapper.Client.CreateContractAsync(request));

    // public Task CreateContract(CreateContractDefinitionRequest contract, CreateContractTemplateRequest? template)
    // {
    //     throw new NotImplementedException();
    // }

    public Task DeleteContract(int id)
    {
        throw new NotImplementedException();
    }

    public Task<ContractListView> GetContract(long id)
    {
        throw new NotImplementedException();
    }
    
    public Task<IEnumerable<ContractTemplateListView>> GetContractTemplates(ContractTemplatesFilter? filter = null) =>
        RetrieveCollection("contract templates",
            () => _wrapper.Client.GetContractTemplatesAsync(filter?.TemplateId, filter?.Name, filter?.Limit, filter?.Offset)
        );

    public Task CreateContractTemplate(CreateContractTemplateRequest request) =>
        Call("Contract template created", "Failed to create contract template", () => _wrapper.Client.CreateContractTemplateAsync(request));
    
    #endregion
    
    #region Datafeeds
    
    public Task<IEnumerable<Datafeed>> GetDatafeeds(PagingFilter filter) =>
        RetrieveCollection("datafeeds", () => _wrapper.Client.GetDatafeedsAsync());

    public Task CreateDatafeed(CreateDatafeedRequest request) =>
        Call("Datafeed created", "Failed to create datafeed", () => _wrapper.Client.CreateDatafeedAsync(request));
    
    #endregion
    
    #region Exchanges
    
    public Task<IEnumerable<Exchange>> GetExchanges(PagingFilter filter) =>
        RetrieveCollection("exchanges", () => _wrapper.Client.GetExchangesAsync());

    public Task CreateExchange(CreateExchangeRequest exchange) =>
        Call("Exchange created", "Failed to create exchange", () => _wrapper.Client.CreateExchangeAsync(exchange));

    public Task DeleteExchange(long id)
    {
        throw new NotImplementedException();
    }
    
    #endregion
    
    #region Streams
    
    public Task<IEnumerable<StreamListView>> GetStreams(StreamsFilter? filter = null) =>
        RetrieveCollection("streams", () => 
            _wrapper.Client.GetStreamsAsync(filter?.Ticker, filter?.ContractId, filter?.StreamIds, filter?.Limit, filter?.Offset)
        );

    public Task CreateStream(CreateStreamRequest request) =>
        Call("Stream created", "Failed to create stream", () => _wrapper.Client.CreateStreamAsync(request));

    public Task CreateStream(Stream stream)
    {
        throw new NotImplementedException();
    }

    public Task DeleteStream(int id)
    {
        throw new NotImplementedException();
    }
    
    #endregion
}