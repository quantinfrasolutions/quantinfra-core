// using IBApi;
// using QuantInfra.Sdk.Trading.ExternalAccounts;
//
// namespace QuantInfra.Connectors.Ibkr.Interfaces;
//
// public class IbkrPosition
// {
//     public IbkrPosition(string account, Contract contract, decimal pos, double avgCost)
//     {
//         Account = account;
//         Contract = contract;
//         Pos = pos;
//         AvgCost = avgCost;
//     }
//
//     public string Account { get; }
//     public Contract Contract { get; }
//     public decimal Pos { get; }
//     public double AvgCost { get; }
//
//     public ExternalPositionReport ToExternalAccountPosition(int accountId, long brokerId) => new()
//     {
//         AccountId = accountId,
//         BrokerId = brokerId,
//         ExternalContractId = Contract.ConId.ToString(),
//         OpenPrice = Convert.ToDecimal(AvgCost),
//         SignedVolume = Pos
//     };
// }