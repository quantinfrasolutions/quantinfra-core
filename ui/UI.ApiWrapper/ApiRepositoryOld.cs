// using Common.Accounting;
// using Common.Accounting.Yield;
// using Common.Accounts.Abstractions;
// using Common.Interfaces.Api;
// using Common.Optimizations;
// using Common.Strategies;
// using Microsoft.Extensions.Logging;
// using OptimizationPipeline;
// using QuantInfra.Common.Accounts.Abstractions;
// using QuantInfra.Common.Accounts.Abstractions.Api;
// using QuantInfra.Common.Books.Abstractions;
// using QuantInfra.Common.Books.Abstractions.Api;
// using QuantInfra.Common.StaticData.Abstractions;
// using QuantInfra.Common.StaticData.Abstractions.Api;
// using QuantInfra.Common.Strategies.Api;
// using Radzen;
// using Services;
// using UI.Interfaces.Accounts;
// using UI.Interfaces.Books;
// using UI.Interfaces.Optimization;
// using UI.Interfaces.StaticData;
// using UI.Interfaces.Strategies;
// using FlattenedBookComponent = Common.Interfaces.Api.Books.FlattenedBookComponent;
//
// namespace UI.ApiWrapper;
//
// public class ApiRepositoryOld :
//     IUIExchangesRepository,
//     IUICommissionsRepository,
//     IUIContractsRepository,
//     IUIBooksRepository,
//     IUIStrategiesRepository,
//     IUIStreamsRepository,
//     IUIAccountsRepository,
//     IUIBrokersRepository,
//     IUICurrenciesRepository,
//     // IUIAssetsRepository,
//     IUIDatafeedsRepository,
//     IStrategyClassesRepository, 
//     IUIOptimizationsRepository
// {
//     private readonly ServiceWrapper _wrapper;
//     private NotificationService _notificationService;
//     private ILogger _logger;
//     
//     public ApiRepositoryOld(ServiceWrapper wrapper, NotificationService notificationService, ILogger logger)
//     {
//         _wrapper = wrapper;
//         _notificationService = notificationService;
//         _logger = logger;
//     }
//
//     #region Exchanges
//
//     public Task<Dictionary<long, Exchange>> GetExchanges(bool refresh = false) =>
//         RetrieveDictionary(
//             "exchanges",
//             _wrapper.ApiClient.GetExchangesAsync,
//             e => e.Id
//         );
//
//     public Task CreateExchange(Exchange exchange) =>
//         Create("exchange", exchange, _wrapper.ApiClient.CreateExchangeAsync);
//
//     public Task DeleteExchange(long id)
//     {
//         throw new NotImplementedException();
//     }
//
//     public async Task<Dictionary<long, TradingSessionModel>>
//         GetTradingSessions(long exchangeId, bool refresh = false) =>
//         await RetrieveDictionary(
//             "trading sessions",
//             exchangeId,
//             _wrapper.ApiClient.GetTradingSessionsAsync,
//             ts => new TradingSessionModel(ts),
//             ts => ts.Id
//         );
//
//     public Task CreateTradingSession(TradingSessionModel value) =>
//         Create("trading session", value, CreateTradingSessionWrapper);
//
//     public Task UpdateContractTradingSessions(long contractId, IEnumerable<long> add, IEnumerable<long> remove) =>
//         Call(
//             "Trading sessions updated",
//             "Failed to update trading sessions",
//             () => _wrapper.ApiClient.UpdateContractTradingSessionsAsync(contractId, new UpdateContractTradingSessionsRequest
//             {
//                 Add = add.ToArray(),
//                 Remove = remove.ToArray()
//             })
//         );
//
//     private Task CreateTradingSessionWrapper(TradingSessionModel ts) =>
//         _wrapper.ApiClient.CreateTradingSessionAsync(ts.ExchangeId, ts.GetTradingSession());
//
//     #endregion
//
//     #region Commissions
//
//     public async Task<Dictionary<long, CommissionStructureView>> GetCommissions(bool refresh = false,
//         long? exchangeId = null,
//         long? brokerId = null, CommissionStructureType? type = null) =>
//         await RetrieveDictionary(
//             "commissions",
//             () => _wrapper.ApiClient.GetCommissionStructuresAsync(exchangeId, brokerId, type.ToString()),
//             cs => cs.Id
//         );
//
//     public Task CreateCommission(CommissionStructure commission) =>
//         Create("commission", commission, _wrapper.ApiClient.CreateCommissionStructureAsync);
//
//     public Task DeleteCommission(long id)
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task UpdateContractCommissions(long contractId, IEnumerable<long> add, IEnumerable<long> remove) =>
//         Call(
//             "Commissions updated",
//             "Failed to update commissions",
//             () => _wrapper.ApiClient.UpdateContractCommissionsAsync(contractId, new UpdateContractTradingSessionsRequest
//             {
//                 Add = add.ToArray(),
//                 Remove = remove.ToArray()
//             })
//         );
//
//     public Task<CommissionStructureView> GetCommission(long id) => Retrieve("commission", id,
//         _wrapper.ApiClient.GetCommissionStructureAsync);
//
//     public Task UpdateCommission(CommissionStructure cs) =>
//         Call(
//             "Commission updated",
//             "Failed updating commission",
//             () => _wrapper.ApiClient.UpdateCommissionStructureAsync(cs.Id, cs)
//         );
//
//     #endregion
//
//     #region Contracts
//
//     public async Task<Dictionary<long, ContractListView>> GetContracts(bool refresh = false, ContractsFilter? filter = null) =>
//         await RetrieveDictionary(
//             "contracts",
//             () => _wrapper.ApiClient.GetContractsAsync(filter?.Ticker, filter?.ExchangeId, filter?.ContractIds ?? new List<long>(), filter?.CommissionId, null, null),
//             c => c.ContractId
//         );
//
//     public async Task<ContractListView> GetContract(long id) =>
//         await Retrieve(
//             "contract",
//             id,
//             _wrapper.ApiClient.GetContractAsync
//         );
//
//     public Task CreateContract(CreateContractDefinitionRequest contract, CreateContractTemplateRequest? template) =>
//         Create("contract", contract, (c) => _wrapper.ApiClient.CreateContractAsync(new CreateContractRequest { ContractDefinition = contract, Template = template}));
//
//     public Task DeleteContract(long id)
//     {
//         throw new NotImplementedException();
//     }
//
//     #endregion
//
//     #region Books
//
//     public Task<Dictionary<Guid, BookDefinition>> GetBooks(bool refresh = false) =>
//         RetrieveDictionary(
//             "books",
//             () => _wrapper.ApiClient.GetBooksAsync(null, null),
//             b => b.BookId
//         );
//     public Task<IEnumerable<SharePriceHistory>> GetBookSharePrice(Guid bookId, bool refresh = false) =>
//         Retrieve("book-sp", bookId, _wrapper.ApiClient.GetBookSharePriceHistoryAsync);
//
//
//     public Task<BookDefinition> GetBook(Guid bookId) =>
//         Retrieve("book", bookId, _wrapper.ApiClient.GetBookAsync);
//
//     public Task CreateBook(CreateBookRequest request) =>
//         Create("book", request, _wrapper.ApiClient.CreateBookAsync);
//
//     public Task UpdateBook(Guid bookId, UpdateBookCompositionRequest request) =>
//         Call(
//             "Book updated",
//             "Error while updating the book",
//             () => _wrapper.ApiClient.UpdateBookCompositionAsync(bookId, request)
//         );
//
//     public async Task<EditableStrategyModel> GetStrategy(Guid strategyId) =>
//         EditableStrategyModel.FromStrategy(await Retrieve("strategy", strategyId, _wrapper.ApiClient.GetStrategyAsync));
//
//     public Task<StrategyTypeDescription> GetStrategyClassDescription(string className) =>
//         Retrieve("description", className, _wrapper.ApiClient.GetStrategyClassAsync);
//
//     public Task<ResultModel> OpenPosition(Guid strategyId, OpenClosePositionRequest request) =>
//         throw new NotImplementedException();
//         // Call("Order placed", "Error while opening position",
//         //     () => _wrapper.ApiClient.Strategies_OpenPositionAsync(strategyId, request));
//
//         public Task<ResultModel> ClosePosition(Guid strategyId, OpenClosePositionRequest request) =>
//             throw new NotImplementedException();
//         // Call("Order placed", "Error while opening position",
//         //     () => _wrapper.ApiClient.Strategies_ClosePositionAsync(strategyId, request));
//
//
//         public Task<IEnumerable<FlattenedBookComponent>> GetBookDistribution(Guid bookId) =>
//             throw new NotImplementedException();
//         // Retrieve("distribution", bookId, _wrapper.ApiClient.Books_GetBookDistributionAsync);
//
//     #endregion
//
//     #region Strategies
//
//     public Task<Dictionary<Guid, EditableStrategyModel>> GetStrategies(bool refresh = false) =>
//         RetrieveDictionary(
//             "strategies",
//             () => _wrapper.ApiClient.GetStrategiesAsync(null, null, null, null, null),
//             s => EditableStrategyModel.FromStrategy(s),
//             s => s.StrategyId
//         );
//
//     public Task CreateStrategy(CreateStrategyRequest strategy) =>
//         Create("strategy", strategy, _wrapper.ApiClient.CreateStrategyAsync);
//
//     public Task StartStrategy(Guid strategyId) =>
//         Call("Strategy started", "Failed to start strategy",
//             () => _wrapper.ApiClient.StartStrategyAsync(strategyId));
//
//     public Task StopStrategy(Guid strategyId, bool force) =>
//         Call("Strategy stopped", "Failed to stop strategy",
//             () => _wrapper.ApiClient.StopStrategyAsync(strategyId, new StopStrategyRequest { ClosePositions = force, Reason = "Manual stop"}));
//
//     #endregion
//
//     #region Helpers
//
//     internal void ProcessException(string name, int? errCode, string? errMsg) =>
//         _notificationService.Notify(NotificationSeverity.Error, $"Error while {name}: {errCode} {errMsg}");
//
//     /// <summary>
//     /// Basic retrieve
//     /// </summary>
//     /// <typeparam name="TApiResult"></typeparam>
//     /// <param name="name"></param>
//     /// <param name="apiMethod"></param>
//     /// <returns></returns>
//     internal async Task<TApiResult> Retrieve<TApiResult>(
//         string name,
//         Func<Task<TApiResult>> apiMethod
//     )
//     {
//         try
//         {
//             return await apiMethod();
//         }
//         catch (Exception ex)
//         {
//             _notificationService.Notify(NotificationSeverity.Error, ex.Message);
//             _logger.LogError(ex, $"Error retrieving {name}");
//             throw;
//         }
//     }
//
//     /// <summary>
//     /// Basic search
//     /// </summary>
//     /// <typeparam name="TSearchKey"></typeparam>
//     /// <typeparam name="TApiResult"></typeparam>
//     /// <param name="name"></param>
//     /// <param name="searchKey"></param>
//     /// <param name="apiMethod"></param>
//     /// <returns></returns>
//     internal async Task<TApiResult> Retrieve<TSearchKey, TApiResult>(
//         string name,
//         TSearchKey searchKey,
//         Func<TSearchKey, Task<TApiResult>> apiMethod
//     )
//     {
//         try
//         {
//             return await apiMethod(searchKey);
//         }
//         catch (Exception ex)
//         {
//             _notificationService.Notify(NotificationSeverity.Error, ex.Message);
//             _logger.LogError(ex, $"Error retrieving {name}");
//             throw;
//         }
//     }
//
//     /// <summary>
//     /// Retrieve and transform
//     /// </summary>
//     /// <typeparam name="TResult"></typeparam>
//     /// <typeparam name="TApiResult"></typeparam>
//     /// <param name="name"></param>
//     /// <param name="apiMethod"></param>
//     /// <param name="conversionMethod"></param>
//     /// <returns></returns>
//     internal async Task<TResult> RetrieveItem<TResult, TApiResult>(
//         string name,
//         Func<Task<TApiResult>> apiMethod,
//         Func<TApiResult, TResult> conversionMethod
//     ) => conversionMethod(await Retrieve(name, apiMethod));
//
//
//     /// <summary>
//     /// Retrieve collection
//     /// </summary>
//     /// <typeparam name="TApiResult"></typeparam>
//     /// <param name="name"></param>
//     /// <param name="apiMethod"></param>
//     /// <returns></returns>
//     internal Task<IEnumerable<TApiResult>> RetrieveCollection<TApiResult>(
//         string name,
//         Func<Task<IEnumerable<TApiResult>>> apiMethod
//     ) => Retrieve(name, apiMethod);
//
//     /// <summary>
//     /// Retrieve and transform collection
//     /// </summary>
//     /// <typeparam name="TResult"></typeparam>
//     /// <typeparam name="TApiResult"></typeparam>
//     /// <param name="name"></param>
//     /// <param name="apiMethod"></param>
//     /// <param name="conversionMethod"></param>
//     /// <returns></returns>
//     internal async Task<IEnumerable<TResult>> RetrieveCollection<TResult, TApiResult>(
//         string name,
//         Func<Task<IEnumerable<TApiResult>>> apiMethod,
//         Func<TApiResult, TResult> conversionMethod
//     ) => (await Retrieve(name, apiMethod)).Select(conversionMethod);
//
//     /// <summary>
//     /// Search and transform
//     /// </summary>
//     /// <typeparam name="TSearchKey"></typeparam>
//     /// <typeparam name="TResult"></typeparam>
//     /// <typeparam name="TApiResult"></typeparam>
//     /// <param name="name"></param>
//     /// <param name="searchKey"></param>
//     /// <param name="apiMethod"></param>
//     /// <param name="conversionMethod"></param>
//     /// <returns></returns>
//     internal async Task<TResult> RetrieveItem<TSearchKey, TResult, TApiResult>(
//         string name,
//         TSearchKey searchKey,
//         Func<TSearchKey, Task<TApiResult>> apiMethod,
//         Func<TApiResult, TResult> conversionMethod
//     ) => conversionMethod(await Retrieve(name, searchKey, apiMethod));
//
//     /// <summary>
//     /// Search and transform collection
//     /// </summary>
//     /// <typeparam name="TSearchKey"></typeparam>
//     /// <typeparam name="TResult"></typeparam>
//     /// <typeparam name="TApiResult"></typeparam>
//     /// <param name="name"></param>
//     /// <param name="searchKey"></param>
//     /// <param name="apiMethod"></param>
//     /// <param name="conversionMethod"></param>
//     /// <returns></returns>
//     internal async Task<IEnumerable<TResult>> RetrieveCollection<TSearchKey, TResult, TApiResult>(
//         string name,
//         TSearchKey searchKey,
//         Func<TSearchKey, Task<IEnumerable<TApiResult>>> apiMethod,
//         Func<TApiResult, TResult> conversionMethod
//     ) => (await Retrieve(name, searchKey, apiMethod)).Select(conversionMethod);
//
//     /// <summary>
//     /// Retrieve, transform and convert to dictionary
//     /// </summary>
//     /// <typeparam name="TApiResult"></typeparam>
//     /// <typeparam name="TResult"></typeparam>
//     /// <typeparam name="TKey"></typeparam>
//     /// <param name="name"></param>
//     /// <param name="apiMethod"></param>
//     /// <param name="conversionMethod"></param>
//     /// <param name="keySelector"></param>
//     /// <returns></returns>
//     internal async Task<Dictionary<TKey, TResult>> RetrieveDictionary<TApiResult, TResult, TKey>(
//         string name,
//         Func<Task<IEnumerable<TApiResult>>> apiMethod,
//         Func<TApiResult, TResult> conversionMethod,
//         Func<TResult, TKey> keySelector
//     ) => (await RetrieveCollection(name, apiMethod, conversionMethod))
//         .ToDictionary(keySelector, i => i);
//
//     /// <summary>
//     /// Search, transform and convert to dictionary
//     /// </summary>
//     /// <typeparam name="TSearchKey"></typeparam>
//     /// <typeparam name="TApiResult"></typeparam>
//     /// <typeparam name="TResult"></typeparam>
//     /// <typeparam name="TKey"></typeparam>
//     /// <param name="name"></param>
//     /// <param name="searchKey"></param>
//     /// <param name="apiMethod"></param>
//     /// <param name="conversionMethod"></param>
//     /// <param name="keySelector"></param>
//     /// <returns></returns>
//     internal async Task<Dictionary<TKey, TResult>> RetrieveDictionary<TSearchKey, TApiResult, TResult, TKey>(
//         string name,
//         TSearchKey searchKey,
//         Func<TSearchKey, Task<IEnumerable<TApiResult>>> apiMethod,
//         Func<TApiResult, TResult> conversionMethod,
//         Func<TResult, TKey> keySelector
//     ) => (await RetrieveCollection(name, searchKey, apiMethod, conversionMethod))
//         .ToDictionary(keySelector, i => i);
//
//     /// <summary>
//     /// Retrieve and convert to dictionary
//     /// </summary>
//     /// <typeparam name="TApiResult"></typeparam>
//     /// <typeparam name="TKey"></typeparam>
//     /// <param name="name"></param>
//     /// <param name="apiMethod"></param>
//     /// <param name="keySelector"></param>
//     /// <returns></returns>
//     internal Task<Dictionary<TKey, TApiResult>> RetrieveDictionary<TApiResult, TKey>(
//         string name,
//         Func<Task<IEnumerable<TApiResult>>> apiMethod,
//         Func<TApiResult, TKey> keySelector
//     ) => RetrieveDictionary(name, apiMethod, x => x, keySelector);
//
//     /// <summary>
//     /// Search and convert to dictionary
//     /// </summary>
//     /// <typeparam name="TSearchKey"></typeparam>
//     /// <typeparam name="TApiResult"></typeparam>
//     /// <typeparam name="TKey"></typeparam>
//     /// <param name="name"></param>
//     /// <param name="searchKey"></param>
//     /// <param name="apiMethod"></param>
//     /// <param name="keySelector"></param>
//     /// <returns></returns>
//     internal Task<Dictionary<TKey, TApiResult>> RetrieveDictionary<TSearchKey, TApiResult, TKey>(
//         string name,
//         TSearchKey searchKey,
//         Func<TSearchKey, Task<IEnumerable<TApiResult>>> apiMethod,
//         Func<TApiResult, TKey> keySelector
//     ) => RetrieveDictionary(name, searchKey, apiMethod, x => x, keySelector);
//
//     /// <summary>
//     /// Basic create
//     /// </summary>
//     /// <typeparam name="TItem"></typeparam>
//     /// <param name="name"></param>
//     /// <param name="item"></param>
//     /// <param name="createFunc"></param>
//     /// <returns></returns>
//     internal async Task Create<TItem>(
//         string name,
//         TItem item,
//         Func<TItem, Task> createFunc
//     )
//     {
//         try
//         {
//             await createFunc(item);
//             _notificationService.Notify(NotificationSeverity.Success, $"{name} created");
//         }
//         catch (Exception ex)
//         {
//             _notificationService.Notify(NotificationSeverity.Error, ex.Message);
//             _logger.LogError(ex, $"Error creating {name}");
//             throw;
//         }
//     }
//
//     internal async Task Call(
//         string successMessage,
//         string errorMessage,
//         Func<Task> func
//     )
//     {
//         try
//         {
//             await func();
//             _notificationService.Notify(NotificationSeverity.Success, successMessage);
//         }
//         catch (Exception ex)
//         {
//             _notificationService.Notify(NotificationSeverity.Error, ex.Message);
//             _logger.LogError(ex, errorMessage);
//             _logger.LogError(ex.InnerException, "Inner exception");
//             throw;
//         }
//     }
//
//     #endregion
//
//     #region Streams
//
//     public Task<Dictionary<long, StreamView>> GetStreams(bool refresh = false, StreamsFilter? filter = null) =>
//         RetrieveDictionary("streams", _wrapper.ApiClient.GetStreamsAsync, s => s.StreamId);
//
//     public Task CreateStream(StreamDefinition stream) =>
//         Create("stream", stream, _wrapper.ApiClient.CreateStreamAsync);
//
//     public Task DeleteStream(long id)
//     {
//         throw new NotImplementedException();
//     }
//     #endregion
//
//     #region Accounts
//     public Task<Dictionary<Guid, AccountListModel>> GetAccounts(bool refresh = false) =>
//         RetrieveDictionary(
//             "accounts",
//             () => _wrapper.ApiClient.GetAccountsAsync(null, null),
//             a => a.AccountId
//         );
//     
//     public Task<Dictionary<Guid, AccountListModel>> GetBrokerAccounts(bool refresh = false) =>
//         RetrieveDictionary(
//             "accounts",
//             () => _wrapper.ApiClient.GetAccountsAsync(null, AccountType.BrokerAccount.ToString()),
//             a => a.AccountId
//         );
//
//     public Task<Dictionary<Guid, AccountListModel>> GetAccounts(AccountsFilter filter)  =>
//         RetrieveDictionary(
//             "accounts",
//             () => _wrapper.ApiClient.GetAccountsAsync(filter.AccountId, filter.AccountType.ToString()),
//             a => a.AccountId
//         );
//
//     public Task<Dictionary<Guid, ExecutableAccountListModel>> GetExecutableAccounts(bool refresh) =>
//         throw new NotSupportedException();
//         // RetrieveDictionary(
//         //     "accounts",
//         //     _wrapper.ApiClient.Accounts_GetExecutableAccountsAsync,
//         //     a => a.AccountId
//         // );
//
//         public Task<ExecutableAccountListModel?> GetExecutableAccount(Guid accountId) =>
//             throw new NotSupportedException();
//         // Retrieve("account", accountId, _wrapper.ApiClient.Accounts_GetExecutableAccountAsync);
//
//         public Task<Dictionary<Guid, ExecutableAccountTargetPositionView>> GetTargetPositions(Guid accountId) =>
//             throw new NotImplementedException();
//         // RetrieveDictionary(
//         //     "target positions",
//         //     () => _wrapper.ApiClient.Accounts_GetTargetPositionsAsync(accountId),
//         //     p => p.PositionId
//         // );
//
//     public Task<IEnumerable<OrderView>> GetActiveOrders(ActiveOrderFilter filter) =>
//         RetrieveCollection("orders", () => _wrapper.ApiClient.GetActiveOrdersAsync(filter));
//     
//     public Task<IEnumerable<OrderView>> GetOrdersHistory(OrderFilter filter) =>
//         RetrieveCollection("orders-history", () => _wrapper.ApiClient.GetOrdersHistoryAsync(filter));
//     
//     public Task<IEnumerable<PositionView>> GetActivePositions(ActivePositionFilter filter) =>
//         RetrieveCollection("positions", () => _wrapper.ApiClient.GetActivePositionsAsync(filter));
//     
//     public Task<IEnumerable<PositionView>> GetPositionsHistory(PositionHistoryFilter filter) =>
//         RetrieveCollection("positions-history", () => _wrapper.ApiClient.GetPositionsHistoryAsync(filter));
//     
//     public Task<IEnumerable<TradeView>> GetTradesHistory(TradeFilter filter) =>
//         RetrieveCollection("trades-history", () => _wrapper.ApiClient.GetTradesHistoryAsync(filter));
//
//     public Task<Dictionary<Guid, AccountListModel>> GetVirtualAccounts(bool refresh) =>
//         RetrieveDictionary(
//             "accounts",
//             () => _wrapper.ApiClient.GetAccountsAsync(null, AccountType.VirtualAccount.ToString()),
//             a => a.AccountId
//         );
//
//     public Task<AccountListModel> GetAccount(Guid accountId) =>
//         Retrieve("account", accountId, _wrapper.ApiClient.GetAccountAsync);
//
//     public Task<Dictionary<long, BalanceModel>> GetBalances(Guid accountId) =>
//         RetrieveDictionary(
//             "balances", 
//             () => _wrapper.ApiClient.GetBalancesAsync(accountId), 
//             b => b.Asset.Id
//         );
//
//     public Task CreateAccount(CreateAccountRequest account) =>
//         Create("account", account, _wrapper.ApiClient.CreateAccountAsync);
//
//     public Task CreateSubscription(Guid accountId, CreateAccountSubscriptionRequest model) =>
//         throw new NotImplementedException();
//         // Create("subscription", model, m => _wrapper.ApiClient.Accounts_SubscribeAsync(accountId, m));
//
//     public Task<IEnumerable<BalanceOperationModel>> GetBalanceOperations(QuantInfra.Common.Accounts.Abstractions.Api.BalanceOperationsFilter filter)
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task CreateBalanceOperation(NewBalanceOperation request) =>
//         Create("balance operation", request, v => _wrapper.ApiClient.CreateBalanceOperationAsync(v.AccountId, v));
//
//     #endregion
//
//     #region Brokers
//
//     public Task<Dictionary<long, Broker>> GetBrokers(bool refresh) =>
//         RetrieveDictionary(
//             "brokers",
//             _wrapper.ApiClient.GetBrokersAsync,
//             b => b.BrokerId
//         );
//
//     #endregion
//
//     #region Currencies
//
//     public Task<Dictionary<long, Currency>> GetCurrencies(bool refresh = false) =>
//         RetrieveDictionary(
//             "currencies",
//             _wrapper.ApiClient.GetCurrenciesAsync,
//             c => c.Id
//         );
//
//     public Task UpdateCurrency(Currency currency)
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task DeleteCurrency(long id)
//     {
//         throw new NotImplementedException();
//     }
//     
//     #endregion
//
//     #region Assets
//     
//     public Task<Dictionary<long, Asset>> GetAssets(bool refresh = false) =>
//         RetrieveDictionary(
//             "assets",
//             _wrapper.ApiClient.GetAssetsAsync,
//             a => a.Id
//         );
//
//     public Task CreateAsset(Asset asset) =>
//         Create("asset", asset, _wrapper.ApiClient.CreateAssetAsync);
//
//     public Task DeleteAsset(long id)
//     {
//         throw new NotImplementedException();
//     }
//     
//     #endregion
//
//     #region Datafeeds
//     
//     public Task<Dictionary<long, Datafeed>> GetDatafeeds(bool refresh = false) =>
//         RetrieveDictionary(
//             "datafeeds",
//             _wrapper.ApiClient.GetDatafeedsAsync,
//             d => d.Id
//         );
//     
//     #endregion
//
//     #region Optimization
//     public Task<Dictionary<long,OptimizationBatch>> GetBatches(bool refresh = false) =>
//         RetrieveDictionary(
//             "optimization-batches",
//             _wrapper.OptimizationClient.Optimization_GetBatchesAsync,
//             d => (long)d.BatchId
//         );
//     
//     public Task<Dictionary<Guid,OptimizationUnit>> GetOptimizationUnits(long optimizationId, bool refresh = false) =>
//         RetrieveDictionary(
//             "optimizations-units",
//            ()=> _wrapper.OptimizationClient.Optimization_GetOptimizationUnitsAsync(optimizationId),
//             d => d.OptimizationUnitId
//         );
//     public Task<IEnumerable<OptimizationPipelineResult>> GetOptimizationResults(OptimizationResultFilter orFilter)=>
//         RetrieveCollection(
//             "optimization-results",
//             () =>
//            _wrapper.OptimizationClient.Optimization_GetOptimizationResultsAsync(orFilter.ResultId, orFilter.ContractId)
//         );
//     public Task<Dictionary<long,StrategyOptimizationParams>> GetOptimizationParams(bool refresh = false) =>
//         RetrieveDictionary(
//             "optimization-params",
//             _wrapper.OptimizationClient.Optimization_GetOptimizationParamsAsync,
//             d => (long)d.GetHashCode()
//         );
//     
//     public Task<Dictionary<Guid,FitnessResult.FitnessTestResult>> GetFitnessesByStrategyId(Guid strategyId)=>
//         RetrieveDictionary(
//             "fitnesses",
//             ()=>_wrapper.OptimizationClient.Optimization_GetFitnessesByStrategyIdAsync(strategyId),
//             d => d.ResultId
//         );
//     
//     public  Task<Dictionary<long,Common.Optimizations.Optimization>> GetOptimizations(bool forceRefresh = false) =>
//          RetrieveDictionary(
//             "optimization",
//             _wrapper.OptimizationClient.Optimization_GetOptimizationsAsync,
//             d =>d.OptimizationId
//         );
//     public Task<IEnumerable<SharePriceHistory>> GetSharePriceByStrategyId(Guid strategyId) =>
//         RetrieveCollection(
//             "sp-history",
//             () =>
//                 _wrapper.OptimizationClient.Optimization_GetSharePriceByStrategyIdAsync(strategyId)
//         );
//     public Task<OptimizationStatistics> GetOptimizationStatistics(long optimizationId, bool forceRefresh = false)=>
//         Retrieve(
//             "optimization-stats",
//             optimizationId, _wrapper.OptimizationClient.Optimization_GetOptimizationStatisticsAsync
//         );
//     public Task CreateOptimization(OptimizationExtended optimization) =>
//         Create("optimization", optimization, _wrapper.OptimizationClient.Optimization_CreateOptimizationAndUnitsAsync);
//     public Task CreateOptimizationUnit(OptimizationUnit unit) =>
//         Create("optimization-unit", unit, _wrapper.OptimizationClient.Optimization_CreateOptimizationUnitAsync);
//     public Task RunOptimization(long optimizationId) =>
//         Create("optimization", optimizationId, _wrapper.OptimizationClient.Optimization_RunOptimizationAsync);
//     public Task CreateBatch(OptimizationBatch batch) =>
//         Create("batch", batch,args=>  _wrapper.OptimizationClient.Optimization_CreateBatchAsync(args));
//     public Task ResetFailedOptimization(long optimizationId) =>
//         Create("optimization", optimizationId, _wrapper.OptimizationClient.Optimization_ResetFailedOptimizationAsync);
//     #endregion
//
//     public Task<Dictionary<string, StrategyTypeDescription>> GetAvailableStrategyClasses(bool refresh) =>
//         RetrieveDictionary(
//             "strategy classes",
//             _wrapper.ApiClient.GetStrategyClassesAsync,
//             c => c.Name
//         );
// }