using System.Collections.Generic;

namespace QuantInfra.Common.Messaging
{
	public class QueueConfig
	{
		public string QueueName { get; set; } = default!;
		public bool Durable { get; set; } = true;
		public bool Exclusive { get; set; } = false;
		public bool AutoDelete { get; set; } = false;
		public List<ExchangeConfig> Bindings { get; set; } = new List<ExchangeConfig>();
		public IDictionary<string, object>? QueueArguments { get; set; }
	}
}

