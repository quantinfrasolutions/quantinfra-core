// using Common.Books.Abstractions;
// using Common.Interfaces.Api.Accounts;
// using Common.StaticData.Abstractions;
//
// namespace UI.Interfaces.Accounts
// {
//     public class AccountListModelView : AccountListModel
// 	{
// 		public SubscriptionModel ActiveSubscriptionModel { get; set; }
//         public Currency Currency { get; set; }
//
//
//         public AccountListModelView() { }
//
// 		public AccountListModelView(AccountListModel account, Dictionary<long, BookDefinition> books = null, Dictionary<long, Currency> currencies = null)
// 			: base(account)
// 		{
// 			if (ActiveSubscription != null) ActiveSubscriptionModel = new SubscriptionModel(ActiveSubscription, books);
//             Currency = currencies?[CurrencyId];
//         }
// 	}
// }
//
