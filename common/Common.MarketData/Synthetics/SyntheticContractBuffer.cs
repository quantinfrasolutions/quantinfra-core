using System.Collections.Generic;
using System.Linq;
using NodaTime;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.StaticData.Synthetics;

namespace QuantInfra.Common.MarketData.Synthetics;

public class SyntheticContractBuffer
{
    private readonly int _streamId;
    private readonly Dictionary<int, decimal> _weights;
    private Dictionary<int, double> _currentPrices;
    private Dictionary<int, double> _currentVolumes;
    private readonly Dictionary<int, double> _lastKnownPrices;
    private Instant? _currentTs = null;
    private readonly ISyntheticPriceCalculator _calculator;

    public SyntheticContractBuffer(int contractId, int? streamId, ISyntheticPriceCalculator calculator, SyntheticContractComposition composition)
    {
        _streamId = streamId ?? -contractId;
        Composition = composition;
        _weights = composition.Weights.ToDictionary(x => x.Key, x => x.Value);
        _calculator = calculator;
        CleanCurrentPrices();
        _lastKnownPrices = new Dictionary<int, double>(_weights.Count);
    }
    
    public ExchangeBar? AppendExchangeBar(ExchangeBar bar, int contractId)
    {
        if (!_weights.ContainsKey(contractId)) return null;
        
        var dt = bar.CloseDt;
        
        if (dt != _currentTs && _currentTs != null)
        {
            var closed = _currentPrices.ToDictionary(x => x.Key, x => x.Value);
            var volumes = _currentVolumes.ToDictionary(x => x.Key, x => x.Value);
            CleanCurrentPrices();
            
            var canBeCalculated = true;
            foreach (var c in _weights.Keys.Except(closed.Keys).ToArray())
            {
                if (_lastKnownPrices.TryGetValue(c, out var lastKnownPrice))
                {
                    closed.Add(c, lastKnownPrice);
                    volumes.Add(c, 0);
                }
                else
                {
                    canBeCalculated = false;
                    break;
                }
            }

            if (canBeCalculated)
            {

                return Calculate(bar, closed, volumes);
            }
        }

        _currentPrices[contractId] = bar.Close;
        _currentVolumes[contractId] = bar.Volume;
        _lastKnownPrices[contractId] = bar.Close;
        _currentTs = dt;

        if (_currentPrices.Count == _weights.Count)
        {
            var prices = _currentPrices;
            var volumes = _currentVolumes;
            CleanCurrentPrices();
            var synthBar = Calculate(bar, prices, volumes);
            LastPrice = synthBar.Close;
            return synthBar;
        }

        return null;
    }

    public SyntheticContractComposition Composition { get; private set; }
    public IReadOnlyDictionary<int, decimal> Weights => _weights;
    public double LastPrice { get; private set; }

    private void CleanCurrentPrices()
    {
        _currentPrices = new Dictionary<int, double>(_weights.Count);
        _currentVolumes = new Dictionary<int, double>(_weights.Count);
        _currentTs = null;
    }

    private ExchangeBar Calculate(ExchangeBar bar, Dictionary<int, double> prices, Dictionary<int, double> volumes)
    {
        var price = _calculator.CalculatePrice(Composition, prices);
        var volume = _calculator.CalculateVolume(Composition, volumes);
        return new ExchangeBar(bar)
        {
            Open = price,
            High = price,
            Low = price,
            Close = price,
            Volume = volume,
            StreamId = _streamId,
            TradingSessionId = null
        };
    }
}