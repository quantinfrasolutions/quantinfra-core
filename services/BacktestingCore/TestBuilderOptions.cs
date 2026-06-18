using System.Collections.Generic;
using Common.Backtesting;
using Common.Strategies;
using NodaTime;
using QuantInfra.Common.Strategies;

namespace BacktestingCore
{
	public class TestBuilderOptions : TestExecutorOptions
	{
		public TestBuilderOptions()
		{
		}
		public TestBuilderOptions(TestBuilderOptions o) : base(o)
		{
			OptimizationUnitId = o.OptimizationUnitId;
			ReturnToComissionThreshold = o.ReturnToComissionThreshold;
			StrategyConfigs = o.StrategyConfigs;
			OptimizationParameters = o.OptimizationParameters;
			OptimizationName = o.OptimizationName;
			BatchId = o.BatchId;
			StartDt = o.StartDt;
			EndDt = o.EndDt;
			ForwardEndDt = o.ForwardEndDt;
			OutOfSampleEndDt = o.OutOfSampleEndDt;
			Action = o.Action;
			ContractId = o.ContractId;
			CostPerShare = o.CostPerShare;
			FloatingCost = o.FloatingCost;
			PnLPath = o.PnLPath;
			FitnessPath = o.FitnessPath;
			TradesPath = o.TradesPath;
			PositionsPath = o.PositionsPath;
			UseDatabase = o.UseDatabase;
			TargetTimeframe = o.TargetTimeframe;
			CandlesFilePath = o.CandlesFilePath;
			TradesThreshold = o.TradesThreshold;
			ConfigPath = o.ConfigPath;
			StaticDataConfigPath = o.StaticDataConfigPath;
			MaxOffset = o.MaxOffset;
			Offsets = o.Offsets;
			PopulationSize = o.PopulationSize;
			NumberOfGenerations = o.NumberOfGenerations;
			CandleType = o.CandleType;
			MMType = o.MMType;
			MTMUtcOffset = o.MTMUtcOffset;
			CandlesTimeframe = o.CandlesTimeframe;
			SharpeThreshold = o.SharpeThreshold;
			PvalueMin = o.PvalueMin;
			PvalueMax = o.PvalueMax;
			UseClosesOnly = o.UseClosesOnly;
			OutputFormat = o.OutputFormat;
			DirectionType = o.DirectionType;
			AverageType = o.AverageType;
			Investment = o.Investment;
			CandlesTimezone = o.CandlesTimezone;
			ProfilingResultsPath = o.ProfilingResultsPath;
			// BarRequestsReserveFactor = o.BarRequestsReserveFactor;
			// AutoUpdateCommissions = o.AutoUpdateCommissions;
			ExchangeId = o.ExchangeId;
			// DaysInYear = o.DaysInYear;
			CommissionsPath = o.CommissionsPath;
			NumberOfBestStrategies = o.NumberOfBestStrategies;
			StabilitySteps = o.StabilitySteps;
			IsSaveResults = o.IsSaveResults;
		}
		public TestBuilderOptions(
			IEnumerable<StrategyConfig>? strategyConfigs,
			Dictionary<string, OptimizationParameter> optimizationParameters,
			string optimizationName,
			int batchId,
			long optimizationUnitId,
			Instant startDt,
			Instant endDt,
			Instant? forwardEndDt = null,
			Instant? outOfSampleEndDt = null,
			string action = "test",
			long contractId = 10000,
			decimal? costPerShare = null,
			decimal? floatingCost = null,
			string pnLPath = "pl.csv",
			string fitnessPath = "fitness.csv",
			string tradesPath = "trades.csv",
			string positionsPath = "positions.csv",
			bool useDatabase = false,
			string candlesFilePath = "candles.csv",
			int tradesThreshold = 0,
			string configPath = "config.json",
			string staticDataConfigPath = "staticData.json",
			bool updateBTS = true,
			int maxOffset = 59,
			int[] offsets = null,
			int populationSize = 0,
			int  numberOfGenerations = 0,
			string candleType = "Candle",
			string mmType = "FixMoneyManagement",
			string mtmUtcOffset = "PT0M",
			string candlesTimeframe = "PT1M",
			string targetTimeframe = "PT60M",
			double sharpeThreshold = 1,
			double pvalueMin = 0.01,
			double pvalueMax = 0.05,
			bool? useClosesOnly =
				null, // When not submitted, it will be derived from the CandlesTimeFrame (true for PT1M). Use this parameter to override.
			OutputFormatFile outputFormat = OutputFormatFile.Parquet,
			string directionType = "LongShort",
			string averageType = "EMA",
			decimal investment = 100000m,
			string candlesTimezone = "UTC", // https://nodatime.org/TimeZones
			string profilingResultsPath = null,
			double barRequestsReserveFactor = 1.5d, // Increase in case of warnings on bars warm up
			bool autoUpdateCommissions = false,
			long exchangeId = long.MinValue,
			int daysInYear = 252,
			string? commissionsPath = null,
			int numberOfBestStrategies = 5,
			int stabilitySteps = 5,
			bool isSaveResults = true,
			int returnToComissionThreshold = 2)
		{
			OptimizationUnitId = optimizationUnitId;
			ReturnToComissionThreshold = returnToComissionThreshold;
			StrategyConfigs = strategyConfigs;
			OptimizationParameters = optimizationParameters;
			OptimizationName = optimizationName;
			BatchId = batchId;
			StartDt = startDt;
			EndDt = endDt;
			ForwardEndDt = forwardEndDt;
			OutOfSampleEndDt = outOfSampleEndDt;
			Action = action;
			ContractId = contractId;
			CostPerShare = costPerShare;
			FloatingCost = floatingCost;
			PnLPath = pnLPath;
			FitnessPath = fitnessPath;
			TradesPath = tradesPath;
			PositionsPath = positionsPath;
			UseDatabase = useDatabase;
			TargetTimeframe = targetTimeframe;
			CandlesFilePath = candlesFilePath;
			TradesThreshold = tradesThreshold;
			ConfigPath = configPath;
			StaticDataConfigPath = staticDataConfigPath;
			MaxOffset = maxOffset;
			Offsets = offsets;
			PopulationSize = populationSize;
			NumberOfGenerations = numberOfGenerations;
			CandleType = candleType;
			MMType = mmType;
			MTMUtcOffset = mtmUtcOffset;
			CandlesTimeframe = candlesTimeframe;
			SharpeThreshold = sharpeThreshold;
			PvalueMin = pvalueMin;
			PvalueMax = pvalueMax;
			UseClosesOnly = useClosesOnly;
			OutputFormat = outputFormat;
			DirectionType = directionType;
			AverageType = averageType;
			Investment = investment;
			CandlesTimezone = candlesTimezone;
			ProfilingResultsPath = profilingResultsPath;
			// BarRequestsReserveFactor = barRequestsReserveFactor;
			// AutoUpdateCommissions = autoUpdateCommissions;
			ExchangeId = exchangeId;
			DaysInYear = daysInYear;
			CommissionsPath = commissionsPath;
			NumberOfBestStrategies = numberOfBestStrategies;
			StabilitySteps = stabilitySteps;
			IsSaveResults = isSaveResults;
		}
		public long OptimizationUnitId { get; set; }
		public string OptimizationName { get; set; }
		public int BatchId { get; set; } 
		public int ReturnToComissionThreshold{ get; set; }
		public IEnumerable<StrategyConfig>? StrategyConfigs{ get; set; }
		public Dictionary<string, OptimizationParameter> OptimizationParameters{ get; set; }
		public Instant? ForwardEndDt { get; set; }
		public Instant? OutOfSampleEndDt { get; set; }
		public string Action { get; set; }
		public decimal? CostPerShare { get; set; }
		public decimal? FloatingCost { get; set; }
		public string PnLPath { get; set; }
		public string FitnessPath { get; set; }
		public bool IsSaveResults { get; set; }
		public string TargetTimeframe { get; set; }
		public string TradesPath { get; set; }
		public string PositionsPath { get; set; }
		public bool UseDatabase { get; set; }
		public string CandlesFilePath { get; set; }
		public int TradesThreshold { get; set; }
		public string ConfigPath { get; set; }
		public string StaticDataConfigPath { get; set; }
		public int MaxOffset { get; set; }
		public int[] Offsets { get; set; }
		public int PopulationSize { get; set; }
		public int NumberOfGenerations { get; set; }
		public string CandleType { get; set; }
		public string MMType { get; set; }
		public string MTMUtcOffset { get; set; }
		public string CandlesTimeframe { get; set; }
		public double SharpeThreshold { get; set; }
		public double PvalueMin { get; set; }
		public double PvalueMax { get; set; }
		public bool? UseClosesOnly { get; set; }
		public OutputFormatFile OutputFormat { get; set; }
		public string DirectionType { get; set; }
		public string AverageType { get; set; }
		public string CandlesTimezone { get; set; }
		public string ProfilingResultsPath { get; set; }
		public long ExchangeId { get; set; }
		public int NumberOfBestStrategies { get; set; }
		public int StabilitySteps { get; set; }
		public string? CommissionsPath { get; set; }
	}
}

