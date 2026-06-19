using QuantInfra.Sdk.Trading.Positions;

namespace QuantInfra.Common.Interfaces.Api.Accounts
{
	public class PositionView : Position
	{        
        public string AccountName { get; set; }
        public string ContractName { get; set; }
        public PositionChangeType? Type { get; set; }
        
        public PositionView() { }
        
        public PositionView(Position p, string accountName, string contractName) : base(p)
        {
            AccountName = accountName;
            ContractName = contractName;
        }

        public PositionView(Position p, string accountName, string contractName, PositionChangeType? type) : base(p)
        {
            AccountName = accountName;
            ContractName = contractName;
            Type = type;
        }
    }
}

