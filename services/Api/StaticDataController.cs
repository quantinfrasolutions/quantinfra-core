using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using QuantInfra.Common.Interfaces.Api.StaticData;
using QuantInfra.Common.StaticData.Abstractions;
using QuantInfra.Databases.Main;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Services.Api;

[ApiController]
[Route("api/static-data")]
public class StaticDataController(MainContext context) : Controller
{
    #region Exchanges
    [HttpGet]
    [Route("exchanges")]
    [EndpointName("GetExchanges")]
    [Produces("application/json")]
    public async Task<IEnumerable<Exchange>> GetExchanges() =>
        await context.Exchanges.AsNoTracking().ToListAsync();            

    [HttpPost]
    [Route("exchanges")]
    [EndpointName("CreateExchange")]
    public async Task<IActionResult> CreateExchange([FromBody] Exchange exchange)
    {
        throw new NotImplementedException();
        // if (!NodaTime.DateTimeZoneProviders.Tzdb.Ids.Contains(exchange.TimezoneName))
        // {
        //     return BadRequest($"{exchange.TimezoneName} is not supported, use Tzdb list");
        // }
        //
        // var e = new Databases.Main.Models.StaticData.Exchange(exchange);
        // e.Id = 0;
        // context.Exchanges.Add(e);
        // await context.SaveChangesAsync();
        // return Ok();
    }

    [HttpPut]
    [Route("exchanges/{exchangeId}")]
    [EndpointName("UpdateExchange")]
    public async Task<IActionResult> UpdateExchange([FromRoute] long exchangeId, [FromBody] Exchange exchangeRequest)
    {
        throw new NotImplementedException();
        // var exchange = await context.Exchanges.FindAsync(exchangeId);
        // if (exchange == null)
        // {
        //     return NotFound(exchangeId);
        // }
        //
        // if (!string.IsNullOrEmpty(exchangeRequest.Name))
        // {
        //     exchange.Name = exchangeRequest.Name;
        // }
        //
        // if (!string.IsNullOrEmpty(exchangeRequest.TimezoneName))
        // {
        //     if (!NodaTime.DateTimeZoneProviders.Tzdb.Ids.Contains(exchangeRequest.TimezoneName))
        //     {
        //         return BadRequest($"{exchange.Timezone} is not supported, use Tzdb list");
        //     }
        //
        //     exchange.Timezone = exchangeRequest.TimezoneName;
        // }
        // await context.SaveChangesAsync();
        // return Ok();
    }
		
    [HttpDelete]
    [Route("exchanges/{exchangeId}")]
    [EndpointName("DeleteExchange")]
    public async Task<IActionResult> DeleteExchange(long exchangeId)
    {
        throw new NotImplementedException();
        // var exchange = await context.Exchanges.FindAsync(exchangeId);
        // if (exchange == null)
        // {
        //              return NotFound(exchangeId);
        // }
        // context.Exchanges.Remove(exchange);
        //          await context.SaveChangesAsync();
        //          return Ok();
    }

    [HttpGet]
    [Route("exchanges/{exchangeId}/trading-sessions")]
    [EndpointName("GetTradingSessions")]
    [Produces("application/json")]
    public async Task<IEnumerable<TradingSession>> GetTradingSessions([FromRoute] long exchangeId)
    {
        throw new NotImplementedException();
        // return await context.GetTradingSessionsAsync(exchangeId);
    }

    [HttpPost]
    [Route("exchanges/{exchangeId}/trading-sessions")]
    [EndpointName("CreateTradingSession")]
    public async Task<IActionResult> CreateTradingSession([FromRoute] long exchangeId,
        [FromBody] TradingSession ts)
    {
        throw new NotImplementedException();
        // await _sdRepository.CreateTradingSessionAsync(ts);
        // return Ok();
    }

    //[HttpPost]
    //[Route("exchanges/{exchangeId}/trading-sessions/{tradingSessionId}")]
    //[Produces("application/json")]
    //public async Task<ResultModel> CreateTradingSessionDay(
    //    [FromRoute] long exchangeId,
    //    [FromRoute] int tradingSessionId,
    //    [FromBody] Common.StaticData.Abstractions.TradingSessionDay tsd
    //)
    //{
    //    var session = _dbContext.TradingSessions.Find(tradingSessionId);
    //    if (session == null)
    //    {
    //        return new ResultModel(1, $"Trading session with id {tradingSessionId} not found");
    //    }

    //    var day = new Databases.Main.Models.StaticData.TradingSessionDay
    //    {
    //        DayOfWeek = tsd.DayOfWeek,
    //        Start = tsd.Start,
    //        End = tsd.End,
    //        TradingSession = session
    //    };
    //    _dbContext.TradingSessionDays.Add(day);

    //    await _dbContext.SaveChangesAsync();
    //    return new ResultModel();
    //}

    [HttpDelete]
    [Route("exchanges/{exchangeId}/trading-sessions/{id}")]
    [EndpointName("DeleteTradingSession")]
    public async Task<IActionResult> DeleteTradingSession([FromRoute] long exchangeId, [FromRoute] int id)
    {
        throw new NotImplementedException();
        // var session = context.TradingSessions.Find(id);
        // if (session == null)
        // {
        //     return NotFound(id);
        // }
        // var days = context.TradingSessionDays.Where(tsd => tsd.TradingSession == session);
        // context.TradingSessionDays.RemoveRange(days);
        // context.TradingSessions.Remove(session);
        // await context.SaveChangesAsync();
        // return Ok();
    }

    #endregion

    #region Streams
    [HttpGet]
    [Route("streams")]
    [EndpointName("GetStreams")]
    [Produces("application/json")]
    public async Task<IEnumerable<StreamListView>> GetStreams([FromQuery] StreamsFilter? filter = null)
    {
        filter ??= new();
        filter.Ticker = filter.Ticker?.ToLower();
            
        return await context
            .Streams
            .Where(c =>
                (filter.ContractId == null || filter.ContractId == c.Contract.ContractId)
                && (filter.StreamIds == null || filter.StreamIds.Count == 0 || filter.StreamIds.Contains(c.StreamId))
                && (string.IsNullOrEmpty(filter.Ticker) || c.Ticker.ToLower().Contains(filter.Ticker))
            )
            .OrderBy(c => c.Ticker)
            .Skip(filter.Offset)
            .Take(filter.Limit)
            .Select(s => new StreamListView(s.StreamId, s.Ticker, s.DatafeedId, null,
                s.Contract.ContractId, s.Contract.Ticker, s.ConstantStreamValue))
            .AsNoTracking()
            .ToListAsync();
    }

    // [HttpPost]
    // [Route("streams")]
    // [EndpointName("CreateStream")]
    // public async Task<IActionResult> CreateStream([FromBody] StreamDefinition stream)
    // {
    //     throw new NotImplementedException();
    //     // stream.StreamId = await context.AddStreamAsync(stream);
    //     //
    //     // if (stream.Enabled)
    //     // {
    //     //     _mdsNotificationQueue.PublishUnwrappedObject(new StreamEnabledChangedEvt(stream.StreamId, true));
    //     // }
    //     //
    //     // return Ok();
    // }

    /// <summary>
    /// Only the following fields can be updated: ContractId, Description, Enabled, VolumeBAU (change to 0 to disable volume aggregation), TradingSessionsId
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="streamRequest"></param>
    /// <returns></returns>
    // [HttpPut]
    // [Route("streams/{streamId}")]
    // [EndpointName("UpdateStream")]
    // public async Task<IActionResult> UpdateStream([FromRoute] long streamId, [FromBody] StreamDefinition streamRequest)
    // {
    //     throw new NotImplementedException();
    //     // var stream = await _sdRepository.GetStreamDefinitionAsync(streamId);
    //     // var enabledChanged = stream.Enabled != streamRequest.Enabled;
    //     // await _sdRepository.UpdateStreamAsync(streamRequest);            
    //     //
    //     // if (enabledChanged)
    //     // {
    //     //     _mdsNotificationQueue.PublishUnwrappedObject(new StreamEnabledChangedEvt(stream.StreamId, stream.Enabled));
    //     // }
    //     //
    //     // return Ok();
    // }
    #endregion

    #region Datafeeds
    [HttpGet, Route("datafeeds")]
    [EndpointName(nameof(GetDatafeeds))]
    [Produces("application/json")]
    public async Task<IEnumerable<Datafeed>> GetDatafeeds()
    {
        return await context.Datafeeds.AsNoTracking().ToListAsync();
    }

    [HttpPost]
    [Route("datafeeds")]
    [EndpointName("CreateDatafeed")]
    public async Task<IActionResult> CreateDatafeed([FromBody] Datafeed datafeed)
    {
        throw new NotImplementedException();
        // var d = datafeed.FromDatafeed();
        // d.DatafeedId = 0;
        // context.Datafeeds.Add(d);
        // await context.SaveChangesAsync();
        // return Ok();
    }

    [HttpPut]
    [Route("datafeeds/{datafeedId}")]
    [EndpointName("UpdateDatafeed")]
    public async Task<IActionResult> UpdateDatafeed([FromRoute] long datafeedId, [FromBody] Datafeed datafeedRequest)
    {
        throw new NotImplementedException();
        // var datafeed = await context.Datafeeds.FindAsync(datafeedId);
        // if (datafeed == null)
        // {
        //     return NotFound(datafeedId);
        // }
        //
        // datafeed.Name = datafeedRequest.Name;
        //
        // await context.SaveChangesAsync();
        // return Ok();
    }

    [HttpDelete]
    [Route("datafeeds/{datafeedId}")]
    [EndpointName("DeleteDatafeed")]
    public async Task<IActionResult> DeleteDatafeed(long datafeedId)
    {
        throw new NotImplementedException();
        // var df = await context.Datafeeds.FindAsync(datafeedId);
        // if (df == null)
        // {
        //     return NotFound(datafeedId);
        // }
        // context.Datafeeds.Remove(df);
        // await context.SaveChangesAsync();
        // return Ok();
    }
    #endregion

    #region Currencies
        
    [HttpGet]
    [Route("currencies")]
    [EndpointName("GetCurrencies")]
    [Produces("application/json")]
    public async Task<IEnumerable<Currency>> GetCurrencies([FromQuery] CurrencyFilter? filter = null)
    {
        filter ??= new();
        if (!string.IsNullOrEmpty(filter.Name)) filter.Name = filter.Name.ToLower();
            
        return await context
            .Currencies
            .Where(c =>
                (filter.Id == null || filter.Id == c.CurrencyId)
                && (string.IsNullOrEmpty(filter.Name) || c.Asset.Name.ToLower().Contains(filter.Name))
            )
            .Include(c => c.Asset)
            .OrderBy(c => c.Asset.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    #endregion
        
    #region Assets
    [HttpGet]
    [Route("assets")]
    [EndpointName("GetAssets")]
    [Produces("application/json")]
    public async Task<IEnumerable<Asset>> GetAssets([FromQuery] AssetFilter? filter = null)
    {
        filter ??= new();
        filter.Name = filter.Name?.ToLower();

        return await context.Assets
            .Where(a =>
                (filter.Id == null || filter.Id == a.AssetId)
                && (string.IsNullOrEmpty(filter.Name) || a.Name.ToLower().Contains(filter.Name))
                && (filter.AssetType == null || filter.AssetType == a.AssetType)
            )
            .OrderBy(a => a.Name)
            .Skip(filter.Offset)
            .Take(filter.Limit)
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpPost]
    [Route("assets")]
    [EndpointName("CreateAsset")]
    public async Task<IActionResult> CreateAsset([FromBody] Asset asset)
    {
        throw new NotImplementedException();
        // var a = asset.FromAsset();
        // a.Id = 0;
        // context.Assets.Add(a);
        // await context.SaveChangesAsync();
        // return Ok();
    }

    [HttpPut]
    [Route("assets/{assetId}")]
    [EndpointName("UpdateAsset")]
    public async Task<IActionResult> UpdateAsset([FromRoute] long assetId, [FromBody] Asset assetRequest)
    {
        throw new NotImplementedException();
        // var asset = await context.Assets.FindAsync(assetId);
        // if (asset == null)
        // {
        //     return NotFound(assetId);
        // }
        //
        // if (!string.IsNullOrEmpty(assetRequest.Name))
        // {
        //     asset.Name = assetRequest.Name;
        // }
        //
        // if (!string.IsNullOrEmpty(assetRequest.Description))
        // {
        //     asset.Description = assetRequest.Description;
        // }
        //
        // await context.SaveChangesAsync();
        // return Ok();
    }

    [HttpDelete]
    [Route("assets/{assetId}")]
    [EndpointName("DeleteAsset")]
    public async Task<IActionResult> DeleteAsset(long assetId)
    {
        throw new NotImplementedException();
        // var asset = await context.Assets.FindAsync(assetId);
        // if (asset == null)
        // {
        //     return NotFound(assetId);
        // }
        // context.Assets.Remove(asset);
        // await context.SaveChangesAsync();
        // return Ok();
    }
    #endregion

    #region ContractTemplates

    [HttpGet]
    [Route("contract-templates")]
    [EndpointName("GetContractTemplates")]
    [Produces("application/json")]
    public async Task<IEnumerable<ContractTemplateListView>> GetContractTemplates([FromQuery] ContractTemplatesFilter? filter = null)
    {
        filter ??= new();
        filter.Name = filter.Name?.ToLower();
            
        return await context
            .ContractTemplates
            .Where(c => 
                (filter.TemplateId == null || filter.TemplateId == c.TemplateId)
                && (string.IsNullOrEmpty(filter.Name) ||  c.Name.ToLower().Contains(filter.Name))
            )
            .OrderBy(t => t.Name)
            .Skip(filter.Offset)
            .Take(filter.Limit)
            .Select(c => new ContractTemplateListView(
                c.TemplateId, c.Name, c.SecurityType, c.PlCalculatorType,
                c.Asset != null ? c.Asset.AssetId : null, c.Asset != null ? c.Asset.Name : null, 
                c.MinSize, c.MinSizeMoney, c.MaxSize, c.MaxSizeMoney,
                c.SizeIncrement, c.TickSize, c.TickValue, c.PriceQuotation, 
                c.SettlementCurrency.CurrencyId, c.SettlementCurrency.Asset.Name,
                c.BaseCurrency != null ? c.BaseCurrency.CurrencyId : null, c.BaseCurrency != null ? c.BaseCurrency.Asset.Name : null,
                c.QuoteCurrency != null ? c.QuoteCurrency.CurrencyId : null, c.QuoteCurrency != null ? c.QuoteCurrency.Asset.Name : null,
                c.DefaultDatafeed != null ? c.DefaultDatafeed.DatafeedId : null, c.DefaultDatafeed != null ? c.DefaultDatafeed.Name : null, 
                c.Exchange.ExchangeId, c.Exchange.Name,
                c.Broker.BrokerId, c.Broker.Name,
                c.DaysInYear, c.Description
            ))
            .AsNoTracking()
            .ToListAsync();
    }

    // [HttpPost]
    // [Route("contract-templates")]
    // [EndpointName("CreateContractTemplate")]
    // public async Task<IActionResult> CreateContractTemplate([FromBody] CreateContractTemplateRequest request)
    // {
    //     throw new NotImplementedException();
    //     // var ct = new Databases.Main.Models.Contracts.ContractTemplate(request);
    //     // context.ContractTemplates.Add(ct);
    //     // await context.SaveChangesAsync();
    //     // return Ok();
    // }

    #endregion
        
    #region Contracts
    [HttpGet]
    [Route("contracts")]
    [EndpointName("GetContracts")]
    [Produces("application/json")]
    public async Task<IEnumerable<ContractListView>> GetContracts([FromQuery] ContractsFilter? filter = null)
    {
        filter ??= new();
        filter.Ticker = filter.Ticker?.ToLower();
            
        if (filter?.CommissionId != null)
        {
            throw new NotImplementedException();
            // var contractIds = (await _context.ContractCommissions
            //         .Where(cc => cc.CommissionId == filter.CommissionId.Value).ToListAsync())
            //     .Select(c => c.ContractTemlpateId).ToArray();
            //
            // if (contractIds.Length == 0) return new List<ContractModel>();
            //
            // return await GetContracts(new ContractsFilter
            // {
            //     ContractIds = contractIds,
            //     ExchangeId = filter.ExchangeId
            // });
        }

        return await context
            .Contracts
            .Where(c =>
                (string.IsNullOrEmpty(filter!.Ticker) || c.Ticker.ToLower().Contains(filter.Ticker))
                && (filter.ExchangeId == null || c.Template.Exchange.ExchangeId == filter.ExchangeId.Value)
                && (filter.ContractIds == null || filter.ContractIds.Count == 0 || filter.ContractIds.Contains(c.ContractId))
            )
            .OrderBy(c => c.Ticker)
            .Skip(filter!.Offset)
            .Take(filter.Limit)
            .Select(c => new ContractListView(
                c.ContractId, c.Ticker,
                new ContractTemplateListView(c.Template.TemplateId, c.Template.Name, c.Template.SecurityType,
                    c.Template.PlCalculatorType,
                    c.Template.Asset.AssetId, c.Template.Asset.Name, c.Template.MinSize, c.Template.MinSizeMoney,
                    c.Template.MaxSize, c.Template.MaxSizeMoney,
                    c.Template.SizeIncrement, c.Template.TickSize, c.Template.TickValue, c.Template.PriceQuotation,
                    c.Template.SettlementCurrency.CurrencyId,
                    c.Template.SettlementCurrency.Asset.Name, c.Template.BaseCurrency.CurrencyId,
                    c.Template.BaseCurrency.Asset.Name,
                    c.Template.QuoteCurrency.CurrencyId, c.Template.QuoteCurrency.Asset.Name,
                    c.Template.DefaultDatafeed.DatafeedId,
                    c.Template.DefaultDatafeed.Name, c.Template.Exchange.ExchangeId, c.Template.Exchange.Name,
                    c.Template.Broker.BrokerId, c.Template.Broker.Name,
                    c.Template.DaysInYear, c.Template.Description
                ),
                c.FirstTradingDate, c.ExpirationDate, c.SyntheticContractType,
                c.SynthRequiresBarRecalculationAtRollover, c.ExternalContractId,
                c.Asset.AssetId, c.Asset.Name, c.Description
            ))
            .AsNoTracking()
            .ToListAsync();
    }

    // [HttpGet]
    // [Route("contracts/{contractId:long}")]
    // [EndpointName("GetContract")]
    // [Produces("application/json")]
    // public async Task<ContractListView> GetContract([FromRoute] long contractId) =>
    //     (await GetContracts(new ContractsFilter { ContractIds = new() { contractId } })).Single();
    //
    // [HttpGet]
    // [Route("contracts/bts")]
    // [EndpointName("GetBaseTradeSizes")]
    // [Produces("application/json")]
    // public async Task<Dictionary<long, BaseTradeSize>> GetBaseTradeSizes() =>
    //     (await _sdRepository.GetCurrentBaseTradeSizesAsync()).ToDictionary(bts => bts.ContractId, bts => bts);
    //
    // [HttpPost]
    // [Route("contracts")]
    // [EndpointName("CreateContract")]
    // public async Task<IActionResult> CreateContract([FromBody] CreateContractRequest request)
    // {
    //     if (request.Template != null)
    //     {
    //         await _sdRepository.CreateContractAsync(request.ContractDefinition.ToContractDefinition(), request.Template.ToContractTemplate());
    //     }
    //     else
    //     {
    //         await _sdRepository.CreateContractAsync(request.ContractDefinition.ToContractDefinition());
    //     }
    //     return Ok();
    // }
    //
    // [HttpPut]
    // [Route("contracts/{contractId}")]
    // [EndpointName("UpdateContract")]
    // public async Task<IActionResult> UpdateContract([FromRoute] long contractid, [FromBody] ContractDefinition contract)
    // {
    //     try
    //     {
    //         await _sdRepository.UpdateContractAsync(contract);
    //         return Ok();
    //     }
    //     catch (Exception ex)
    //     {
    //         return BadRequest(ex);
    //     }
    // }
    //
    // [HttpPut]
    // [Route("contracts/{contractId:long}/trading-sessions")]
    // [EndpointName("UpdateContractTradingSessions")]
    // public async Task<IActionResult> UpdateContractTradingSessions([FromRoute] long contractId,
    //     [FromBody] UpdateContractTradingSessionsRequest request)
    // {
    //     foreach (var a in request.Add)
    //     {
    //         context.TradingSessionsToContractsMapping.Add(new TradingSessionsToContractsMapping
    //         {
    //             TemplateId = contractId,
    //             TradingSessionId = a
    //         });
    //     }
    //
    //     if (request.Remove.Length > 0)
    //     {
    //         var mappings = await context
    //             .TradingSessionsToContractsMapping
    //             .Where(ts => 
    //                 ts.TemplateId == contractId &&
    //                 request.Remove.Contains(ts.TradingSessionId))
    //             .ToListAsync();
    //         
    //         context.TradingSessionsToContractsMapping.RemoveRange(mappings);
    //     }
    //
    //     await context.SaveChangesAsync();
    //     return Ok();
    // }
    //
    // [HttpPut]
    // [Route("contracts/{contractId:long}/commissions")]
    // [EndpointName("UpdateContractCommissions")]
    // public async Task<IActionResult> UpdateContractCommissions([FromRoute] long contractId,
    //     [FromBody] UpdateContractTradingSessionsRequest request)
    // {
    //     foreach (var a in request.Add)
    //     {
    //         context.ContractCommissions.Add(new ContractCommission
    //         {
    //             ContractTemlpateId = contractId,
    //             CommissionId = a
    //         });
    //     }
    //
    //     if (request.Remove.Length > 0)
    //     {
    //         var mappings = await context
    //             .ContractCommissions
    //             .Where(cs => 
    //                 cs.ContractTemlpateId == contractId &&
    //                 request.Remove.Contains(cs.CommissionId))
    //             .ToListAsync();
    //         
    //         context.ContractCommissions.RemoveRange(mappings);
    //     }
    //
    //     await context.SaveChangesAsync();
    //     return Ok();
    // }
    //
    // [HttpDelete]
    // [Route("contracts/{contractId}")]
    // [EndpointName("DeleteContract")]
    // public async Task<IActionResult> DeleteContract(long contractId)
    // {
    //     try
    //     {
    //         await _sdRepository.DeleteContractAsync(contractId);
    //         return Ok();
    //     }
    //     catch (Exception ex)
    //     {
    //         return BadRequest(ex);
    //     }
    // }
    #endregion
        
    #region Commission Structures
    [HttpGet]
    [Route("commission-structures")]
    [EndpointName("GetCommissionStructures")]
    [Produces("application/json")]
    public async Task<IEnumerable<CommissionStructure>> GetCommissionStructures([FromQuery] CommissionsFilter? filter)
    {
        filter ??= new();
        if (!string.IsNullOrEmpty(filter.Name)) filter.Name = filter.Name.ToLower();

        return await context
            .Commissions
            .Where(cs =>
                (filter.CommissionId == null || cs.CommissionId == filter.CommissionId.Value)
                && (filter.Type == null || cs.CommissionStructureType == filter.Type)
                && (filter.CurrencyId == null || cs.Currency.CurrencyId == filter.CurrencyId.Value)
                && (filter.ExchangeId == null || cs.ExchangeId == filter.ExchangeId.Value)
                && (filter.BrokerId == null || cs.BrokerId == filter.BrokerId.Value)
                && (string.IsNullOrEmpty(filter.Name) || cs.Name.Contains(filter.Name))
            )
            // .Include(cs => cs.Exchange)
            // .Include(cs => cs.Broker)
            .OrderBy(ts => ts.Name)
            .Skip(filter.Offset)
            .Take(filter.Limit)
            .AsNoTracking()
            .ToListAsync();
    }

    //
    // [HttpGet]
    // [Route("commission-structures/{id:long}")]
    // [EndpointName("GetCommissionStructure")]
    // [Produces("application/json")]
    // public async Task<CommissionStructureView> GetCommissionStructure([FromRoute] long id) =>
    //     (await context
    //         .CommissionStructures
    //         .Where(cs => cs.Id == id)
    //         .Include(cs => cs.Exchange)
    //         .Include(cs => cs.Broker)
    //         .AsNoTracking()
    //         .ToListAsync()
    //     )
    //     .Select(cs => new CommissionStructureView(cs, cs.Exchange?.Name, cs.Broker?.Name))
    //     .Single();
    //
    // [HttpPost]
    // [Route("commission-structures")]
    // [EndpointName("CreateCommissionStructure")]
    // public async Task<IActionResult> CreateCommissionStructure([FromBody] CommissionStructure commissionStructure)
    // {
    //     var cs = new Databases.Main.Models.Contracts.CommissionStructure(commissionStructure);
    //     cs.Id = 0;
    //     context.CommissionStructures.Add(cs);
    //     await context.SaveChangesAsync();
    //     return Ok();
    // }
    //
    // [HttpPut]
    // [Route("commission-structures/{id:long}")]
    // [EndpointName("UpdateCommissionStructure")]
    // public async Task<IActionResult> UpdateCommissionStructure([FromRoute] long id, [FromBody] CommissionStructure commissionStructure)
    // {
    //     var cs = await context.CommissionStructures.Where(cs => cs.Id == id).SingleAsync();
    //     cs.BrokerId = commissionStructure.BrokerId;
    //     cs.ExchangeId = commissionStructure.ExchangeId;
    //     cs.CommissionStructureType = commissionStructure.CommissionStructureType;
    //     cs.FixedPerShare = commissionStructure.FixedPerShare;
    //     cs.Floating = commissionStructure.Floating;
    //     cs.Name = commissionStructure.Name;
    //     cs.Description = commissionStructure.Description;
    //     await context.SaveChangesAsync();
    //     return Ok();
    // }
    //
    // [HttpDelete]
    // [Route("commission-structures/{commissionStructureId}")]
    // [EndpointName("DeleteCommissionStructure")]
    // public async Task<IActionResult> DeleteCommissionStructure(long commissionStructureId)
    // {
    //     var cs = await context.CommissionStructures.FindAsync(commissionStructureId);
    //     if (cs == null)
    //     {
    //         return NotFound(commissionStructureId);
    //     }
    //     context.CommissionStructures.Remove(cs);
    //     await context.SaveChangesAsync();
    //     return Ok();
    // }
    #endregion
        
    #region Brokers
    [HttpGet]
    [Route("brokers")]
    [EndpointName(nameof(GetBrokers))]
    [Produces("application/json")]
    public async Task<IReadOnlyCollection<Broker>> GetBrokers([FromQuery] BrokersFilter? filter = null)
    {
        filter ??= new();
        return await context.Brokers.Skip(filter.Offset).Take(filter.Limit).ToListAsync();
    }
        
    #endregion
}