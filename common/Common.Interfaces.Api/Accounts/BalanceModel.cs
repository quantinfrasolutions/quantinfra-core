namespace QuantInfra.Common.Interfaces.Api.Accounts
{
	public class BalanceModel
	{
		public long AssetId { get; init; }
		public string AssetName { get; init; }
		public decimal Balance { get; set; }

		public BalanceModel() { }

		public BalanceModel(long assetId, string assetName, decimal balance)
		{
			AssetId = assetId;
			AssetName = assetName;
			Balance = balance;
		}
	}
}

