using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Common.Interfaces.Api.Accounts
{
    public class AccountListModel : AccountRecordV6
	{
		public string CurrencyName { get; init; }
		public string? BrokerName { get; init; }
		public BrokerType? BrokerType { get; init; }
		public string? ExternalAccountId { get; init; }
		public int? StrategyId { get; init; }
		public string? StrategyName { get; set; }
		// public BookSubscriptionModel? BookSubscription { get; init; }
		

		public AccountListModel() { }

		public AccountListModel(AccountRecordV6 account, string currencyName, string? brokerName, BrokerType? brokerType, string? externalAccountId,
			int? strategyId, string? strategyName
			// , BookSubscriptionModel? bookSubscription
		) : base(account)
		{
			CurrencyName = currencyName;
			BrokerName = brokerName;
			BrokerType = brokerType;
			StrategyId = strategyId;
			StrategyName = strategyName;
			ExternalAccountId = externalAccountId;
			// BookSubscription = bookSubscription;
		}
	}
}

