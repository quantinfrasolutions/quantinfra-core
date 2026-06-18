using System;

namespace QuantInfra.Common.EventSourcing
{
	public record class CommandResult(
		Guid RequestId,
		bool IsSuccess,
		string? Error,
		object? Result
	)
	{
		public static CommandResult Success(Guid requestId, object? result = null) => new CommandResult(requestId, true, string.Empty, result);
		public static CommandResult Fail(Guid requestId, string error) => new CommandResult(requestId, false, error, null);		
	};
}

