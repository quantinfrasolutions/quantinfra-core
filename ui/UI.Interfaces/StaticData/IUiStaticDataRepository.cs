using QuantInfra.Common.Interfaces.Api;
using QuantInfra.Common.Interfaces.Api.StaticData;
using QuantInfra.Sdk.StaticData;

namespace UI.Interfaces.StaticData;

public interface IUiStaticDataRepository
{
    Task<IEnumerable<AssetView>> GetAssets(AssetFilter? filter = null);
    Task CreateAsset(CreateAssetRequest asset);
    Task DeleteAsset(long id);
    
    Task<IEnumerable<Broker>> GetBrokers(BrokersFilter? filter = null);
    
    Task<IEnumerable<CommissionStructure>> GetCommissions(CommissionsFilter filter);
    Task CreateCommission(CreateCommissionRequest request);
    Task DeleteCommission(int id);
    Task UpdateContractCommissions(int contractId, IEnumerable<int> add, IEnumerable<int> remove);
    // Task<CommissionStructureView> GetCommission(long id);
    Task UpdateCommission(CommissionStructure cs);
    
    Task<IEnumerable<ContractListView>> GetContracts(ContractsFilter? filter = null);
    Task CreateContract(CreateContractRequest request);
    Task DeleteContract(int id);
    Task<IEnumerable<ContractTemplateListView>> GetContractTemplates(ContractTemplatesFilter? filter = null);
    Task CreateContractTemplate(CreateContractTemplateRequest request);
    
    Task<IEnumerable<Datafeed>> GetDatafeeds(PagingFilter filter);
    Task CreateDatafeed(CreateDatafeedRequest request);
    
    public Task<IEnumerable<Exchange>> GetExchanges(PagingFilter filter);
    public Task CreateExchange(CreateExchangeRequest exchange);
    public Task DeleteExchange(long id);
    // Task<Dictionary<long, TradingSessionModel>> GetTradingSessions(long exchangeId, bool refresh = false);
    // Task CreateTradingSession(TradingSessionModel value);
    // Task UpdateContractTradingSessions(long contractId, IEnumerable<long> add, IEnumerable<long> remove);
    
    public Task<IEnumerable<StreamListView>> GetStreams(StreamsFilter? filter = null);
    public Task CreateStream(CreateStreamRequest request);
    public Task DeleteStream(int id);
}