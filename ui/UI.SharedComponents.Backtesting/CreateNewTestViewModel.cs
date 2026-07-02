// using System.Text.Json;
// using System.Text.Json.Serialization;
// using NodaTime;
// using NodaTime.Serialization.SystemTextJson;
// using QuantInfra.Common.Interfaces.Api.Strategies;
// using QuantInfra.Sdk.Backtesting;
//
// namespace UI.SharedComponents.Backtesting;
//
// public class CreateNewTestViewModel
// {
//     public IReadOnlyCollection<string> Actions { get; set; }
//     public NewTestModel? NewTestModel { get; set; }
//     public string? StrategyParsingError { get; set; } 
//     public string? RunError { get; set; }
//     public IReadOnlyCollection<RequiredMarketDataUnit>? MdReqs { get; set; }
//     internal CreateNewTest Component { get; set; }
//
//     public bool IsLoading, CanRun, IsRunning;
//     public BacktestingUnitStatus? Status = null;
//     public ActionResult? Results;
//     public Guid? UnitId;
//
//     internal async Task LoadActions()
//     {
//         Actions = await Session.Workspace.TestServer.GetSupportedActionsAsync();
//     }
//
//     private const string PreferencesKey = "new-test-params";
//     bool _configLoaded;
//     internal void TryLoadLastRunConfig()
//     {
//         if (_configLoaded) return;
//         
//         if (Preferences.ContainsKey(PreferencesKey))
//         {
//             var saved = Preferences.Get(PreferencesKey, null);
//             if (saved is not null)
//             {
//                 try
//                 {
//                     NewTestModel = JsonSerializer.Deserialize<NewTestModel>(saved, _jsonOptions.Value);
//                     if (NewTestModel is not null) return;
//                 }
//                 catch
//                 {
//                     Preferences.Remove(PreferencesKey);
//                 }
//             }
//         }
//         
//         NewTestModel ??= new();
//         _configLoaded = true;
//     }
//     
//     private static readonly Lazy<JsonSerializerOptions> _jsonOptions = new(() =>
//     {
//         var options = new JsonSerializerOptions()
//         {
//             PropertyNameCaseInsensitive = true,
//             PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//             WriteIndented = true,
//             AllowTrailingCommas = true,
//         };
//         options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
//         options.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
//         return options;
//     });
//     
//     internal void ParseStrategyConfigJson(string json)
//     {
//         try
//         {
//             NewTestModel.Strategies.Clear();
//             StrategyParsingError = null;
//             var request = JsonSerializer.Deserialize<CreateStrategyRequest>(json, _jsonOptions.Value);
//             if (request is null) throw new InvalidOperationException("Cannot parse CreateStrategyRequest");
//             NewTestModel.Strategies.Add(request);
//         }
//         catch (Exception e)
//         {
//             StrategyParsingError = e.Message;
//         }
//     }
//
//     internal async Task ValidateMarketData()
//     {
//         if (Session.Workspace is null) return;
//
//         try
//         {
//             IsLoading = true;
//             MdReqs = await Session.Workspace.TestServer.ValidateRequiredMarketData(NewTestModel.TestExecutorOptions,
//                 NewTestModel.Strategies);
//             CanRun = true;
//         }
//         catch (Exception ex)
//         {
//             RunError = ex.Message;
//         }
//         finally
//         {
//             IsLoading = false;
//         }
//     }
//
//     internal async Task RunTest()
//     {
//         RunError = null;
//         Results = null;
//         if (Session.Workspace is null) return;
//         
//         Preferences.Set("new-test-params", JsonSerializer.Serialize(NewTestModel, _jsonOptions.Value));
//         
//         if (IsRunning) return;
//         IsRunning = true;
//         IsLoading = true;
//         try
//         {
//             var unit = new BacktestingUnit(Guid.NewGuid(), NewTestModel.Action!, NewTestModel.TestExecutorOptions, NewTestModel.Strategies);
//             UnitId = unit.UnitId; 
//             await Session.Workspace.TestServer.RunAsync(unit);
//             await Task.Run(async () =>
//             {
//                 do
//                 {
//                     Status = await Session.Workspace.TestServer.GetStatusAsync(UnitId!.Value);
//                     MainThread.BeginInvokeOnMainThread(Component.Update);
//                     await Task.Delay(1000);
//                 } while (Status is null ||
//                          (Status.Status != Common.Backtesting.Status.Finished && Status.Status != Common.Backtesting.Status.Canceled && Status.Status != Common.Backtesting.Status.Failed));
//             });
//             if (Status is not null)
//             {
//                 if (Status.Status == Common.Backtesting.Status.Finished)
//                 {
//                     Results = await Session.Workspace.TestServer.GetResultAsync(UnitId!.Value);
//                     await Session.Workspace.TestServer.DeleteResultAsync(UnitId!.Value);
//                     
//                 }
//                 else if (Status.Status == Common.Backtesting.Status.Failed)
//                 {
//                     RunError = Status.Error;
//                 }
//             }
//         }
//         catch (Exception e)
//         {
//             RunError = e.Message;
//         }
//         finally
//         {
//             IsLoading = false;
//             IsRunning = false;
//             UnitId = null;
//         }
//     }
//
//     internal Task SaveTestOptions()
//     {
//         if (Session.Workspace is null) return Task.CompletedTask;
//         return Session.Workspace.SaveTestAsync(new(null, SystemClock.Instance.GetCurrentInstant(), 
//             NewTestModel.Action, new(NewTestModel.TestExecutorOptions), NewTestModel.Strategies.ToList()));
//     }
//
//     internal Task CancelRun()
//     {
//         if (UnitId is null) return Task.CompletedTask;
//         if (Session?.Workspace is null) return Task.CompletedTask;
//         return Session.Workspace.TestServer.CancelExecutionAsync(UnitId.Value);
//     }
// }
//
// // public class NewTestModel
// // {
// //     public NewTestModel() { }
// //
// //     [JsonConstructor]
// //     public NewTestModel(string action, TestExecutorOptions? testExecutorOptions, List<CreateStrategyRequest>? strategies)
// //     {
// //         Action = action;
// //         TestExecutorOptions = testExecutorOptions ?? new();
// //         Strategies = strategies ?? new();
// //     }
// //     
// //     public string Action { get; set; }
// //     public TestExecutorOptions TestExecutorOptions { get; init; } = new();
// //     public List<CreateStrategyRequest> Strategies { get; init; } = new();
// // }