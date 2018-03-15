using System;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Vostok.Hosting;
using Vostok.Logging;
using Vostok.Logging.Serilog;

namespace AntiPlagiarism.Web
{
    public static class EntryPoint
    {
        public static void Main(string[] args)
        {
			BuildVostokHost(args).Run();
		}

        public static IVostokHost BuildVostokHost(params string[] args)
        {
            return new VostokHostBuilder<WebApplication>()
                .SetServiceInfo("ulearn", "AntiPlagiarism.Web")
                .ConfigureAppConfiguration(configurationBuilder =>
                {
                    configurationBuilder.AddCommandLine(args);
                    configurationBuilder.AddEnvironmentVariables();
                    configurationBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureHost((context, hostConfigurator) =>
                {
                    var loggerConfiguration = new LoggerConfiguration().MinimumLevel.Debug();
					
                    if (context.Configuration.GetSection("hostLog").GetValue<bool>("console"))
                    {
                        loggerConfiguration = loggerConfiguration
                            .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss.fff} {Level:u3} [{Thread}] {Message:l}{NewLine}{Exception}", restrictedToMinimumLevel: LogEventLevel.Information);
                    }
                    var pathFormat = context.Configuration.GetSection("hostLog")["pathFormat"];
                    if (!string.IsNullOrEmpty(pathFormat))
					{
						var minimumLevelString = context.Configuration.GetSection("hostLog").GetValue<string>("minimumLevel", "debug");
						if (!Enum.TryParse(minimumLevelString, true, out LogEventLevel minimumLevel) || !Enum.IsDefined(typeof(LogEventLevel), minimumLevel))
							minimumLevel = LogEventLevel.Debug;
						
                        loggerConfiguration = loggerConfiguration
                            .WriteTo.RollingFile(
								pathFormat,
								outputTemplate: "{Timestamp:HH:mm:ss.fff} {Level:u3} [{Thread}] {Message:l}{NewLine}{Exception}",
								restrictedToMinimumLevel: minimumLevel  
								);
                    }
                    var hostLog = new SerilogLog(loggerConfiguration.CreateLogger());
                    hostConfigurator.SetHostLog(hostLog);
                })
                .ConfigureAirlock((context, configurator) =>
                {
                    configurator.SetLog(context.HostingEnvironment.Log.FilterByLevel(LogLevel.Error));
                })
                .Build();
        }
    }
}