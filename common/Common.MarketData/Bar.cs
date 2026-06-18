using System.Collections.Generic;
using System.Collections.ObjectModel;
using NodaTime;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Common.MarketData
{
    public class Bar : IBar
    {
        private readonly Dictionary<int, double?> _indicatorValues;
        private readonly Dictionary<int, object?> _indicatorsData;


        public Bar(int indicatorsCount)
        {
            _indicatorValues = new(indicatorsCount);
            _indicatorsData = new();
        }

        public Instant OpenDt { get; init; }
        public Instant CloseDt { get; init; }
        public int? TradingSessionId { get; init; }
        public bool OpensTradingSession { get; init; }
        //public bool Closed { get; set; }

        public double? this[int key]
        {
            get => _indicatorValues.ContainsKey(key) ? _indicatorValues[key] : null;
            set => _indicatorValues[key] = value;
        }

        public string BarToLogFormat =>
            $"'dt':'{this.CloseDt}', 'o':{this.Open},'h':{this.High}," +
            $"'l':{this.Low},'c':{this.Close},'v':{this.Volume}";

        public void AppendIndicator(int k, double? v) =>
            // _indicatorValues.Add(k, v);
            _indicatorValues[k] = v;
        
        public void AppendIndicatorData(int k, object? d) =>
            _indicatorsData.Add(k, d);

        public void AppendIndicator(int k, double? v, object d)
        {
            AppendIndicator(k, v);
            AppendIndicatorData(k, d);
        }

        public object? GetIndicatorData(int id) =>
            _indicatorsData.ContainsKey(id) ? _indicatorsData[id] : null;

        public ReadOnlyDictionary<int, double?> GetIndicatorValues() =>
            new (_indicatorValues);

        // TODO: check, why they are nullable here
        public double? Open => this["Open".GetHashCode()];
		public double? High => this["High".GetHashCode()];
		public double? Low => this["Low".GetHashCode()];
		public double? Close => this["Close".GetHashCode()];
		public double? Volume => this["Volume".GetHashCode()];

        public ExchangeBar ToExchangeBar(int streamId, int? contractId) => new ExchangeBar(streamId, contractId,
            OpenDt, CloseDt, Open ?? 0, High ?? 0, Low ?? 0, Close ?? 0, Volume ?? 0, 0, 0, TradingSessionId);
    }
}