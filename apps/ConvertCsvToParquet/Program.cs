using ConsoleAppFramework;
using QuantInfra.Core.Apps.ConvertCsvToParquet;

var app = ConsoleApp.Create();
app.Add<Commands>();
app.Run(args);