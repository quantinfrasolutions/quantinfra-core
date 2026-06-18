using System;
using System.Text.Json.Serialization;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.Trading.ExternalAccounts;

namespace QuantInfra.Domain.Commands.Accounts.AccountsService;

public record ProcessExternalExecutionReportCmd(
    ExternalExecutionReport ExecutionReport,
    Guid RequestId
) : ICommand
{
    [JsonConstructor]
    public ProcessExternalExecutionReportCmd(ExternalExecutionReport ExecutionReport) : 
        this(ExecutionReport, Guid.NewGuid()) { }
}