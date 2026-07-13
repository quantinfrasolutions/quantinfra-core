using Common.StaticData.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;
using QuantInfra.Common.Interfaces.Api.StaticData;
using QuantInfra.Databases.Main;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading;
using Stream = QuantInfra.Sdk.StaticData.Stream;

namespace QuantInfra.Core.Services.Api.StaticData;

[ApiController]
[Route("api/static-data")]
public class StaticDataController(MainContext context, IStaticDataRepositoryReadOnly sdRepository) : Controller
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
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateExchange([FromBody] CreateExchangeRequest request)
    {
        if (!NodaTime.DateTimeZoneProviders.Tzdb.Ids.Contains(request.TimezoneName))
            ModelState.AddModelError(nameof(request.TimezoneName), "Invalid timezone name. Use tzdb list.");
        
        var existingExchange = await context.Exchanges.SingleOrDefaultAsync(e => e.Name.ToLower() == request.Name.ToLower());
        if (existingExchange != null) ModelState.AddModelError(nameof(request.Name), $"Duplicate exchange name ({existingExchange.ExchangeId})");
        
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var exchange = request.ToExchange();
        context.Exchanges.Add(exchange);
        await context.SaveChangesAsync();
        return Ok();
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

    [HttpPost, Route("exchanges/{exchangeId}/trading-sessions")]
    [EndpointName("CreateTradingSession")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
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

    [HttpPost, Route("streams")]
    [EndpointName(nameof(CreateStream))]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateStream([FromBody] CreateStreamRequest request)
    {
        if (string.IsNullOrEmpty(request.Ticker)) ModelState.AddModelError(nameof(request.Ticker), "Ticker is required");
        var existingStream = await context.Streams.FirstOrDefaultAsync(s => s.Ticker == request.Ticker);
        if (existingStream != null) ModelState.AddModelError(nameof(request.Ticker), $"Duplicate ticker ({existingStream.StreamId})");

        if (request.DatafeedId == 0) ModelState.AddModelError(nameof(request.DatafeedId), $"Datafeed is required");
        else
        {
            var df = await context.Datafeeds.SingleOrDefaultAsync(d => d.DatafeedId == request.DatafeedId);
            if (df is null) ModelState.AddModelError(nameof(request.DatafeedId), "Datafeed doesn't exist");
        }
        
        Contract? contract = null;
        if (request.ContractId.HasValue)
        {
            contract = await context.Contracts.SingleOrDefaultAsync(c => c.ContractId == request.ContractId.Value);
            if (contract is null) ModelState.AddModelError(nameof(request.ContractId), $"Contract doesn't exist");
        }
        
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var stream = new Stream
        {
            Ticker = request.Ticker,
            DatafeedId = request.DatafeedId,
            Contract = contract,
            ConstantStreamValue = request.ConstantValue.HasValue
                ? new ConstantStreamValue { Value = request.ConstantValue.Value }
                : null
        };
        context.Streams.Add(stream);
        
        await context.SaveChangesAsync();
        return CreatedAtAction(
            nameof(GetStreams),
            null,
            stream.StreamId
        );
    }

    [HttpPost, Route("streams/{streamId:int}/csv")]
    [EndpointName(nameof(SetConstantValueStream))]
    public async Task<IActionResult> SetConstantValueStream([FromRoute] int streamId, [FromBody] SetConstantValueStreamRequest request)
    {
        var stream = await context.Streams
            .Include(s => s.ConstantStreamValue)
            .SingleOrDefaultAsync(s => s.StreamId == streamId);
        if (stream is null) return NotFound();

        if (stream.ConstantStreamValue is not null) context.ConstantStreams.Remove(stream.ConstantStreamValue);

        var csv = new ConstantStreamValue()
        {
            StreamId = streamId,
            Value = request.Value,
        };
        context.ConstantStreams.Add(csv);
        await context.SaveChangesAsync();
        return Ok();
    }
    
    [HttpDelete, Route("streams/{streamId:int}/csv")]
    [EndpointName(nameof(DeleteConstantValueStream))]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteConstantValueStream([FromRoute] int streamId)
    {
        var stream = await context.Streams
            .Include(s => s.ConstantStreamValue)
            .SingleOrDefaultAsync(s => s.StreamId == streamId);
        if (stream is null) return NotFound();

        if (stream.ConstantStreamValue is null) return BadRequest("Constant value is not set for stream"); 
        context.ConstantStreams.Remove(stream.ConstantStreamValue);
        await context.SaveChangesAsync();
        return Ok();
    }

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

    [HttpPost, Route("datafeeds")]
    [EndpointName(nameof(CreateDatafeed))]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDatafeed([FromBody] CreateDatafeedRequest request)
    {
        if (string.IsNullOrEmpty(request.Name)) ModelState.AddModelError(nameof(request.Name), "Name is required");
        else
        {
            var existingDf = await context.Datafeeds.SingleOrDefaultAsync(df => df.Name == request.Name);
            if (existingDf is not null) ModelState.AddModelError(nameof(request.Name), $"Duplicate name ({existingDf.DatafeedId})");
        }
        
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        
        var df = new Datafeed { DatafeedId = 0, Name = request.Name };
        context.Datafeeds.Add(df);
        await context.SaveChangesAsync();
        
        return CreatedAtAction(
            nameof(GetDatafeeds),
            null,
            df.DatafeedId
        );
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
        
    [HttpGet, Route("currencies")]
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
    public async Task<IEnumerable<AssetView>> GetAssets([FromQuery] AssetFilter filter)
    {
        filter.Name = filter.Name?.ToLower();

        return (
            await context.Assets
                .Where(a =>
                    (filter.Id == null || filter.Id == a.AssetId)
                    && (string.IsNullOrEmpty(filter.Name) || a.Name.ToLower().Contains(filter.Name))
                    && (filter.AssetType == null || (AssetType)filter.AssetType == a.AssetType)
                )
                .GroupJoin(
                    context.Currencies.Include(c => c.BrokerOverrides),
                    asset => asset.AssetId,
                    currency => currency.CurrencyId,
                    (asset, currencies) => new
                    {
                        Asset = asset,
                        Currency = currencies.SingleOrDefault()
                    }
                )
                .OrderBy(a => a.Asset.Name)
                .Skip(filter.Offset)
                .Take(filter.Limit)
                .AsNoTracking()
                .ToListAsync()
            )
            .Select(x => new AssetView(
                x.Asset,
                x.Currency?.Decimals,
                x.Currency?.BrokerOverrides.Any() ?? false,
                Array.Empty<string>()
            ));
    }

    [HttpPost, Route("assets")]
    [EndpointName("CreateAsset")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsset([FromBody] CreateAssetRequest request)
    {
        if (string.IsNullOrEmpty(request.Name)) ModelState.AddModelError(nameof(request.Name), "Name is required");
        var existingAsset = await context.Assets.SingleOrDefaultAsync(a => a.Name.ToLower() == request.Name.ToLower());
        if (existingAsset != null) ModelState.AddModelError(nameof(request.Name), $"Duplicate asset name {existingAsset.Name} ({existingAsset.AssetId})");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        
        var asset = request.ToAsset();
        context.Assets.Add(asset);

        if (asset.AssetType == AssetType.Currency)
        {
            var currency = new Currency
            {
                Asset = asset,
                Decimals = request.Decimals,
            };
            context.Currencies.Add(currency);
        }
        await context.SaveChangesAsync();
        return Ok();
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
                c.TemplateId, c.Name, c.SecurityType, c.PnLCalculatorType,
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

    [HttpPost]
    [Route("contract-templates")]
    [EndpointName(nameof(CreateContractTemplate))]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateContractTemplate([FromBody] CreateContractTemplateRequest request)
    {
        if (string.IsNullOrEmpty(request.Name)) ModelState.AddModelError(nameof(request.Name), $"Name is required");
        var existingTemplate = await context.ContractTemplates.AsNoTracking().SingleOrDefaultAsync(c => c.Name == request.Name);
        if (existingTemplate is not null) ModelState.AddModelError(nameof(request.Name), $"Duplicate name ({existingTemplate.TemplateId})");

        if (request is { PlCalculatorType: PnLCalculatorType.Futures or PnLCalculatorType.InverseFutures, TickValue: null }) 
            ModelState.AddModelError(nameof(request.TickValue), $"TickValue is required for futures");

        Asset? asset = null;
        if (request.AssetId.HasValue)
        {
            asset = await context.Assets.SingleOrDefaultAsync(a => a.AssetId == request.AssetId.Value);
            if (asset == null) ModelState.AddModelError(nameof(request.AssetId), $"Asset {request.AssetId} not found");
        }

        Currency? settlCcy = null;
        if (request.SettlementCurrencyId == 0) ModelState.AddModelError(nameof(request.SettlementCurrencyId), "Settlement currency is required");
        else
        {
            settlCcy = await context.Currencies.SingleOrDefaultAsync(c => c.CurrencyId == request.SettlementCurrencyId);
            if (settlCcy == null) ModelState.AddModelError(nameof(request.SettlementCurrencyId), $"Currency {request.SettlementCurrencyId} not found");
        }
        
        Currency? baseCcy = null, quoteCcy = null;
        if (request.BaseCurrencyId == 0) ModelState.AddModelError(nameof(request.BaseCurrencyId), "Settlement currency is required");
        else
        {
            settlCcy = await context.Currencies.SingleOrDefaultAsync(c => c.CurrencyId == request.SettlementCurrencyId);
            if (settlCcy == null) ModelState.AddModelError(nameof(request.SettlementCurrencyId), $"Currency {request.SettlementCurrencyId} not found");
        }

        Datafeed? df = null;
        if (request.DefaultDatafeedId.HasValue)
        {
            df = await context.Datafeeds.SingleOrDefaultAsync(d => d.DatafeedId == request.DefaultDatafeedId);
            if (df == null) ModelState.AddModelError(nameof(request.DefaultDatafeedId), $"Datafeed {request.DefaultDatafeedId} not found");
        }

        List<CommissionStructure> commissions = new();
        if (request.CommissionIds.Count > 0)
        {
            commissions = await context.Commissions.Where(c => request.CommissionIds.Contains(c.CommissionId)).ToListAsync();
            var missingCommissions = request.CommissionIds.Except(commissions.Select(c => c.CommissionId)).ToList();
            if (missingCommissions.Any()) ModelState.AddModelError(nameof(request.CommissionIds), $"Commissions {string.Join(", ", missingCommissions)} not found");
        }

        List<TradingSession> tradingSessions = new();
        if (request.TradingSessionsIds.Count > 0)
        {
            tradingSessions = await context.TradingSessions.Where(c => request.TradingSessionsIds.Contains(c.TradingSessionId)).ToListAsync();
            var missingTradingSessions = request.TradingSessionsIds.Except(tradingSessions.Select(c => c.TradingSessionId)).ToList();
            if (missingTradingSessions.Any()) ModelState.AddModelError(nameof(request.TradingSessionsIds), $"TradingSessions {string.Join(", ", missingTradingSessions)} not found");
        }

        Exchange? exchange = null;
        if (request.ExchangeId == 0) ModelState.AddModelError(nameof(request.ExchangeId), $"Exchange is required");
        else
        {
            exchange = await context.Exchanges.SingleOrDefaultAsync(e => e.ExchangeId == request.ExchangeId);
            if (exchange == null) ModelState.AddModelError(nameof(request.ExchangeId), $"Exchange {request.ExchangeId} not found");
        }

        Broker? broker = null;
        if (request.BrokerId == 0) ModelState.AddModelError(nameof(request.BrokerId), $"Broker is required");
        else
        {
            broker = await context.Brokers.SingleOrDefaultAsync(b => b.BrokerId == request.BrokerId);
            if (broker == null)
                ModelState.AddModelError(nameof(request.BrokerId), $"Broker {request.BrokerId} not found");
        }

        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        
        var ct = new ContractTemplate(0, request.Name, request.SecurityType, asset, request.MinSize, request.MinSizeMoney,
            request.MaxSize, request.MaxSizeMoney, request.SizeIncrement, request.TickSize, request.TickValue ?? request.TickSize, request.PriceQuotation,
            settlCcy!, request.PlCalculatorType, baseCcy, quoteCcy, 
            df, commissions, tradingSessions, exchange!, broker!, request.DaysInYear, request.Description);
        context.ContractTemplates.Add(ct);
        await context.SaveChangesAsync();
        return Ok();
    }

    #endregion
        
    #region Contracts
    [HttpGet]
    [Route("contracts")]
    [EndpointName("GetContracts")]
    [Produces("application/json")]
    public async Task<IEnumerable<ContractListView>> GetContracts([FromQuery] ContractsFilter? filter)
    {
        filter ??= new();
        return await sdRepository.GetContractsAsync(filter);
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
    [HttpPost, Route("contracts")]
    [EndpointName(nameof(CreateContract))]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateContract([FromBody] CreateContractRequest request)
    {
        if (string.IsNullOrEmpty(request.Ticker)) ModelState.AddModelError(nameof(request.Ticker), "Ticker is required");
        var existingContract = await context.Contracts.SingleOrDefaultAsync(c => c.Ticker == request.Ticker);
        if (existingContract != null) ModelState.AddModelError(nameof(request.Ticker), $"Duplicate ticker ({existingContract.ContractId})");

        var template = await context.ContractTemplates.Include(t => t.Asset)
            .SingleOrDefaultAsync(t => t.TemplateId == request.TemplateId);
        if (template == null) ModelState.AddModelError(nameof(request.TemplateId), $"Template {request.TemplateId} not found");
        
        if (template is not null && template.Asset is null && request.AssetId is null)
            ModelState.AddModelError(nameof(request.AssetId), $"Template doesn't contain as asset, so it must be specified in the contract");

        Asset? asset = null;
        if (request.AssetId.HasValue)
        {
            asset = await context.Assets.SingleOrDefaultAsync(a => a.AssetId == request.AssetId.Value);
            if (asset == null) ModelState.AddModelError(nameof(request.AssetId), $"Asset {request.AssetId} not found");
        }

        LocalDate? firstTradingDate = null, expirationDate = null;
        if (!string.IsNullOrEmpty(request.FirstTradingDate))
            firstTradingDate = LocalDatePattern.Iso.Parse(request.FirstTradingDate).Value;
        if (!string.IsNullOrEmpty(request.ExpirationDate))
            expirationDate = LocalDatePattern.Iso.Parse(request.ExpirationDate).Value;
        
        var datafeed = await context.Datafeeds.SingleOrDefaultAsync(c => c.DatafeedId == request.DefaultDatafeedId);
        if (datafeed is null) ModelState.AddModelError(nameof(request.DefaultDatafeedId), $"Datafeed {request.DefaultDatafeedId} not found");
        
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var contract = new Contract(0, request.Ticker, template!, firstTradingDate, expirationDate, request.SyntheticContractType,
            request.SynthRequiresBarRecalculationAtRollover, null, request.ExternalContractId, asset, request.Description, null,
            request.DefaultDatafeedId
        );
        context.Contracts.Add(contract);
        
        await context.SaveChangesAsync();

        var streamCollision = await context.Streams.SingleOrDefaultAsync(s => s.StreamId == contract.ContractId);
        
        var stream = new Stream
        {
            StreamId = streamCollision is null ? contract.ContractId : 0,
            Contract = contract,
            DatafeedId = contract.DefaultDatafeedId,
            Ticker = contract.Ticker,
        };
        context.Streams.Add(stream);
        
        await context.SaveChangesAsync();
        return CreatedAtAction(
            nameof(GetContracts),
            null,
            contract.ContractId
        );
    }
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
    public async Task<IEnumerable<CommissionStructure>> GetCommissionStructures([FromQuery] CommissionsFilter filter)
    {
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
    
    [HttpPost, Route("commission-structures")]
    [EndpointName(nameof(CreateCommissionStructure))]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCommissionStructure([FromBody] CreateCommissionRequest request)
    {
        if (string.IsNullOrEmpty(request.Name)) ModelState.AddModelError(nameof(request.Name), "Name is required");
        else
        {
            var existingComm = await context.Commissions.SingleOrDefaultAsync(c => c.Name == request.Name);
            if (existingComm != null) ModelState.AddModelError(nameof(request.Name), $"Duplicate name ({existingComm.CommissionId})");
        }

        Currency? currency = null;
        if (request.FixedPerShare != 0)
        {
            if (request.CurrencyId == 0) ModelState.AddModelError(nameof(request.CurrencyId), "Currency is required for fixed commissions");
            else
            {
                currency = await context.Currencies.SingleOrDefaultAsync(c => c.CurrencyId == request.CurrencyId);
                if (currency is null) ModelState.AddModelError(nameof(request.CurrencyId), $"Currency {request.CurrencyId} not found");
            }
        }

        Broker? broker = null;
        if (request.BrokerId.HasValue)
        {
            broker = await context.Brokers.SingleOrDefaultAsync(b => b.BrokerId == request.BrokerId.Value);
            if (broker is null) ModelState.AddModelError(nameof(request.BrokerId),  $"Broker {request.BrokerId.Value} not found");
        }
        
        Exchange? exchange = null;
        if (request.ExchangeId.HasValue)
        {
            exchange = await context.Exchanges.SingleOrDefaultAsync(b => b.ExchangeId == request.ExchangeId.Value);
            if (broker is null) ModelState.AddModelError(nameof(request.ExchangeId),  $"Exchange {request.ExchangeId.Value} not found");
        }
        
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var comm = new CommissionStructure()
        {
            CommissionId = 0,
            CommissionStructureType = request.CommissionStructureType,
            BrokerId = request.BrokerId,
            Currency = currency,
            Description = request.Description,
            ExchangeId = request.ExchangeId,
            FixedPerShare = request.FixedPerShare,
            Floating = request.Floating,
            Name = request.Name,
        };
        context.Commissions.Add(comm);
        await context.SaveChangesAsync();
        return CreatedAtAction(
            nameof(GetCommissionStructures),
            null,
            comm.CommissionId
        );
    }
    
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