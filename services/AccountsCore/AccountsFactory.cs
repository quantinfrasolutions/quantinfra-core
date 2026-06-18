using System;
using System.Collections.Generic;
using AccountsCore;
using Microsoft.Extensions.Logging;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Account.Execution.State;
using QuantInfra.Domain.Accounts.Base;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Accounts.Execution;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Services.AccountsCore.State;

namespace QuantInfra.Services.AccountsCore;

public class AccountsFactory :
	IQueryHandler<GetAccount, IAccount?>,
	IQueryHandler<GetAccountBase, AccountBase?>,
	IQueryHandler<GetAccount, IBrokerAccount?>,
	IConfigurableLoggingModule
	// , IQueryHandler<GetFundAccount, IFundAccount>
{
	private readonly Dictionary<int, AccountBase> _accounts = new();

	private readonly Config _config;
	private readonly IEventBus _eventBus;
	private readonly IQueryBus _queryBus;
	private readonly ILoggerFactory _loggerFactory;
	private readonly AccountServiceState _state;
	
	private bool _loggingEnabled = true;


	public AccountsFactory(
		Config config,
		IEventBus eventBus,
		IQueryBus queryBus,
		ILoggerFactory loggerFactory,
		AccountServiceState state
	)
	{
		_config = config;
		_eventBus = eventBus;
		_queryBus = queryBus;
		_loggerFactory = loggerFactory;
		_state = state;
	}
	
	public void EnableLogging()
	{
		_loggingEnabled = true;
		foreach (var account in _accounts.Values)
		{
			account.EnableLogging();
		}
	}

	public void DisableLogging()
	{
		_loggingEnabled = false;
		foreach (var account in _accounts.Values)
		{
			account.DisableLogging();
		}
	}
	

	public AccountBase? GetAccount(int accountId)
	{
		if (!_accounts.ContainsKey(accountId))
		{
			var accountRecord = _queryBus.Query<GetAccount, AccountRecordV6?>(new(accountId));
			if (accountRecord is null) return null;
			var accountState = _state.AccountStates.GetValueOrDefault(accountId);
			if (accountState is null) return null;
			InstantiateAccount(accountRecord, accountState);
		}
		
		return _accounts[accountId];
	}

	// public ICompositeAccount GetCompositeAccount(Guid accountId) => (ICompositeAccount)GetAccount(accountId);
	//
	// public IExecutableSubaccount GetExecutableSubaccount(Guid accountId) =>
	// 	(IExecutableSubaccount)GetAccount(accountId);
	//
	// public IBrokerAccount GetBrokerAccount(Guid accountId) => (BrokerAccount)GetAccount(accountId);
	//
	// public IStrategySubaccount GetStrategySubaccount(Guid accountId) => 
	// 	(IStrategySubaccount)GetAccount(accountId);
	//
	// public IFundAccount GetFund(Guid accountId) => (Fund)GetAccount(accountId);
	
	public IAccount? Handle(GetAccount query) => GetAccount(query.AccountId);
	AccountBase IQueryHandler<GetAccountBase, AccountBase>.Handle(GetAccountBase query) => GetAccount(query.AccountId);
	
	private AccountBase InstantiateAccount(AccountRecordV6 account, AccountBaseState state)
	{
		AccountBase acc;
		
		switch (account.AccountType)
		{
			case AccountType.VirtualAccount:
				acc = new VirtualAccount(account, state, _state, _state, _state, _state, _state, _eventBus, _queryBus, _loggerFactory, _config.LogLevel);
				break;
			case AccountType.BrokerAccount:
				acc = new BrokerAccount(account, (BrokerAccountState)state, _state, _state, _state, _state, _state, _eventBus, _queryBus, _loggerFactory, _config.LogLevel);
				break;
			case AccountType.StrategySubAccount:
				acc = new StrategySubaccount(account, state, _state, _state, _state, _state, _state, _eventBus, _queryBus, _loggerFactory, _config.LogLevel);
				break;
			// case AccountType.ExecutableSubAccount:
			// 	acc = new ExecutableSubaccount(_accountsRepository.GetExecutableSubaccountState(account.AccountId)!, _eventBus, _queryBus, _accountsRepository,
			// 		_loggerFactory, this, _staticDataProvider); // TODO
			// 	break;
			// case AccountType.Fund:
			// 	acc = new Fund(_accountsRepository.GetFundAccountState(account.AccountId)!, _eventBus, _queryBus, _accountsRepository, _loggerFactory, _staticDataProvider);
			// 	break;
			default:
				throw new NotImplementedException();
		}
		
		if (!_loggingEnabled) acc.DisableLogging();
		
		_accounts[acc.AccountId] = acc;
		return acc;
	}

	// public IFundAccount Handle(GetFundAccount request) => GetFund(request.AccountId);
	//
	// public async Task<IFundAccount> HandleAsync(GetFundAccount request) => GetFund(request.AccountId);

	IBrokerAccount IQueryHandler<GetAccount, IBrokerAccount>.Handle(GetAccount query) =>
		(BrokerAccount)GetAccount(query.AccountId);
}

public record GetAccountBase(int AccountId) : IQuery<AccountBase?>;