using Microsoft.Extensions.Configuration;
using QuantInfra.Services.LocalTestServer;

namespace QuantInfra.Core.Apps.StrategyTesterCli;

record GlobalOptions(IConfiguration Configuration);