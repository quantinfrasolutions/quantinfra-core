using System;

namespace QuantInfra.Connectors.Ibkr.Common
{
	public class IbkrException : Exception
	{
		public IbkrException(int code, string message) : base($"IBKR error: code={code}, message={message}")
		{
			ErrorCode = code;
			ErrorMessage = message;
		}		

		public int ErrorCode { get; }
		public string ErrorMessage { get; }
	}
}

