using System;
using System.Collections.Generic;
using System.Linq;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Services.MarketData.TradeAggregators;

namespace QuantInfra.Services.MarketData
{
	internal class Calculator
	{
		private readonly Dictionary<int, MinuteTradeAggregator> _timeAggregators;
		private readonly Dictionary<int, VolumeTradeAggregator> _volumeAggregators;
		private readonly Dictionary<int, Stream> _streams;		


        public Calculator(IEnumerable<Stream> enabledStreams)
		{
			var arr = enabledStreams as Stream[] ?? enabledStreams.ToArray();
			
			_timeAggregators = arr
				.ToDictionary(s => s.StreamId, s => new MinuteTradeAggregator());
			// _volumeAggregators = arr
			// 	.Where(s => s.VolumeBAU != 0)
			// 	.ToDictionary(s => s.StreamId, s => new VolumeTradeAggregator(s.VolumeBAU));
			_streams = arr
				.ToDictionary(s => s.StreamId, s => s);
		}


		public Dictionary<int, Stream> ActiveStreams =>
			_streams.ToDictionary(i => i.Key, i => i.Value);

		internal Dictionary<int, ExchangeBar?> GetCurrentAggregations() =>
			_timeAggregators.ToDictionary(a => a.Key, a => a.Value?.GetCurrentAggregation());


		public (ExchangeBar?, ExchangeBar?) AppendTrade(ExchangeTrade t)
		{
			ExchangeBar? time = null, volume = null;

			var streamId = t.StreamId;

            if (_timeAggregators.ContainsKey(streamId))
			{
				time = _timeAggregators[streamId].AppendTrade(t);
			}
			if (_volumeAggregators.ContainsKey(streamId))
			{
				volume = _volumeAggregators[streamId].AppendTrade(t);
			}
			return (time, volume);
		}

		public (ExchangeBar[]?, ExchangeBar[]?) AppendBar(ExchangeBar b)
		{
			ExchangeBar[]? time = null, volume = null;			

			var streamId = b.StreamId;

            if (_timeAggregators.TryGetValue(streamId, out var aggregator))
            {
                time = aggregator.AppendBar(b);
            }
            // if (_volumeAggregators.TryGetValue(streamId, out var volumeAggregator))
            // {
            //     volume = volumeAggregator.AppendBar(b);
            // }

			// if (time != null)
			// {
			// 	foreach (var t in time) t.StreamId = streamId;				
			// }
			// if (volume != null)
			// {
			// 	foreach (var v in volume) v.StreamId = streamId;
			// }

            return (time, volume);
        }

		public void AddStream(Stream stream)
		{
			if (!_streams.ContainsKey(stream.StreamId))
			{
				_streams.Add(
					stream.StreamId,
					stream
				);
				_timeAggregators.Add(
					stream.StreamId,
					new MinuteTradeAggregator()
				);
			}
			// if (stream.VolumeBAU != 0 && _volumeAggregators.ContainsKey(stream.StreamId))
			// {
			// 	_volumeAggregators.Add(
			// 		stream.StreamId,
			// 		new VolumeTradeAggregator(stream.VolumeBAU)
			// 	);
			// }
		}

		public void UpdateBAU(int streamId, double bau)
		{
			throw new NotImplementedException();
			if (_streams.ContainsKey(streamId))
			{				
				if (bau != 0 && _volumeAggregators.ContainsKey(streamId))
				{
					throw new ArgumentException("Cannot change BAU, only enabling / disabling supported");
				}
				if (bau != 0)
				{
					_volumeAggregators.Add(streamId, new VolumeTradeAggregator(bau));
				}
				else
				{
					_volumeAggregators.Remove(streamId);
				}
			}
		}

		public void RemoveStream(int streamId)
		{
			if (_streams.ContainsKey(streamId))
			{				
				_timeAggregators.Remove(streamId);
				// if (_volumeAggregators.ContainsKey(streamId))
				// {
				// 	_volumeAggregators.Remove(streamId);
				// }
			}
		}		
	}
}
