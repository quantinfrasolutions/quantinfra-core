using System;

namespace QuantInfra.Common.EventSourcing
{
	public interface ICommand
	{
		Guid RequestId { get; }
	}
}

