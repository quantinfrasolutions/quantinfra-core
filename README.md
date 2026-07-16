# README

# QuantInfra Core

**QuantInfra** is an open trading infrastructure platform for building, testing, deploying, and operating systematic trading strategies.

The platform provides a unified architecture that covers the entire strategy lifecycle from historical research and backtesting to live execution and portfolio management, enabling quantitative traders and investment firms to build institutional-grade trading systems without assembling dozens of disconnected components.

The platform is multi-asset and multi-account. Every strategy trades on its own subaccount and sees only its own trades and positions. A single broker account can host multiple strategies, and each strategy may use multiple broker accounts to trade contracts offered by different brokers.

> **⚠️ Beta Release**
> 
> 
> This is the first public beta release of QuantInfra. While the platform is already fully functional for many use cases, APIs and functionality may evolve before the first stable release. Feedback, bug reports, and feature requests are highly appreciated.
> 

---

## Currently available

Current beta includes support for:

- Static data management — centralized database for all contracts you trade
- Market data management — subscriptions for real-time data from different venues and a centralized database to store it
- Event-driven C# SDK to develop strategies
- Live execution
- Stock, futures, and crypto trading
- History database for the full history of orders, trades, positions
- Real-time PnL calculation
- Binance USDⓈ-M Futures connector
- Historical backtesting
- Live strategy execution
- UI for managing strategies and accounts and viewing reports

---

## Documentation

The complete documentation is available in the QuantInfra Knowledge Base:

- [https://quantinfra.gitbook.io/quantinfra-docs/](https://quantinfra.gitbook.io/quantinfra-docs/)

Useful starting points:

- **Getting Started** [https://quantinfra.gitbook.io/quantinfra-docs/tutorials/getting-started](https://quantinfra.gitbook.io/quantinfra-docs/tutorials/getting-started)
- **Backtester Installation** [https://quantinfra.gitbook.io/quantinfra-docs/installation/tester](https://quantinfra.gitbook.io/quantinfra-docs/installation/tester)
- **Trading Engine Installation** [https://quantinfra.gitbook.io/quantinfra-docs/installation/trading-engine](https://quantinfra.gitbook.io/quantinfra-docs/installation/trading-engine)
- **Strategy Implementation Guide** [https://quantinfra.gitbook.io/quantinfra-docs/strategies/implementing-strategies](https://quantinfra.gitbook.io/quantinfra-docs/strategies/implementing-strategies)

---

## Roadmap

The current development roadmap includes:

### Phase 1

- Complete Binance exchange coverage
    - USDⓈ-M Futures
    - COIN-M Futures
    - Spot
- Interactive Brokers connector

### Future phases

- Additional exchange and broker connectors
- Tick-level backtesting with realistic execution simulation
- Continued platform performance and usability improvements

---

## Repository overview

The QuantInfra open-source ecosystem currently consists of three repositories:

### QuantInfra Core (this repository)

The core trading platform contains:

- High-performance event-driven backtesting
- Live trading engine
- Market data management
- Infrastructure services
- Exchange connectors

### SDK

[https://github.com/quantinfrasolutions/quantinfra-sdk](https://github.com/quantinfrasolutions/quantinfra-sdk)

Contains the public SDK used to develop trading strategies and integrations.

### Standard Indicators

[https://github.com/quantinfrasolutions/quantinfra-standard-indicators](https://github.com/quantinfrasolutions/quantinfra-standard-indicators)

A collection of commonly used technical indicators implemented on top of the SDK.

---

## Commercial Edition

In addition to the open-source platform, QuantInfra is available as a commercial offering providing features designed for professional trading teams, hedge funds, proprietary trading firms, and fintech companies.

More information:

- [https://www.quantinfra.solutions](https://www.quantinfra.solutions)

---

## Reporting issues

If you encounter a bug, have a feature request, or would like to suggest an improvement, please create an issue in this repository.

Community feedback is one of the most valuable inputs during the beta period.

---

## License

This repository is licensed under the custom source-available [license](https://github.com/quantinfrasolutions/quantinfra-core/blob/main/LICENSE). It allows unlimited production use and modifications of the core engine for internal use inside your organization.

The SDK and Standard Indicators repositories are released under the Apache 2.0 License, allowing you to develop proprietary trading strategies while retaining full ownership of your code.