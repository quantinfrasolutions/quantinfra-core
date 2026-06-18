// using Common.Accounts.Abstractions;
// using QuantInfra.Common.Accounts.Abstractions;
// using QuantInfra.Common.Accounts.Abstractions.AccountStates;
//
// namespace QuantInfra.Tests.Mocks;
//
// public class MockAccountsFactory : IAccountsFactory
// {
//     public Dictionary<Guid, IAccount> Accounts { get; } = new();
//     
//     public IAccount TryInstantiateAndGetAccount(AccountRecord accountRecord)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IAccount GetAccount(Guid accountId) => Accounts[accountId];
//
//     public ICompositeAccount GetCompositeAccount(Guid accountId) => (ICompositeAccount)Accounts[accountId];
//
//     // public IStrategySubaccount GetStrategySubaccount(Guid accountId) => (IStrategySubaccount)Accounts[accountId];
//
//     public IExecutableSubaccount GetExecutableSubaccount(Guid accountId) => (IExecutableSubaccount)Accounts[accountId];
//
//     public IFundAccount GetFund(Guid accountId) => (IFundAccount)Accounts[accountId];
//
//     public IBrokerAccount GetBrokerAccount(Guid accountId) => (IBrokerAccount)Accounts[accountId];
// }