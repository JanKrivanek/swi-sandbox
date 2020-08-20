using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;

using Xunit.Abstractions;

namespace SolarWinds.UniversalPolling.Prototyping.Tests.Utils
{
    class LoggerHelper
    {
        public static ILoggerFactory CreateLoggerFactory(ITestOutputHelper testOutputHelper, LogEventLevel logEventLevel = LogEventLevel.Verbose)
        {
            var levelSwitch = new LoggingLevelSwitch { MinimumLevel = logEventLevel };
            LoggerConfiguration configuration = new LoggerConfiguration()
                                                .MinimumLevel.ControlledBy(levelSwitch)
                                                .WriteTo.TestOutput(testOutputHelper)
                                                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Hosting.Diagnostics"))
                                                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Server.Kestrel"));

            var factory = new LoggerFactory();

            factory.AddSerilog(configuration.CreateLogger());

            return factory;
        }
    }
}
