using Database;
using Database.Models;
using Database.Repos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Vostok.Commons.Extensions.UnitConvertions;
using Vostok.Hosting;
using Vostok.Instrumentation.AspNetCore;
using Vostok.Logging.Serilog;
using Vostok.Logging.Serilog.Enrichers;
using Vostok.Metrics;

namespace Web.Api
{
    public class WebApplication : AspNetCoreVostokApplication
    {
        protected override void OnStarted(IVostokHostingEnvironment hostingEnvironment)
        {
            hostingEnvironment.MetricScope.SystemMetrics(1.Minutes());
        }

        protected override IWebHost BuildWebHost(IVostokHostingEnvironment hostingEnvironment)
        {
            var loggerConfiguration = new LoggerConfiguration()
                .Enrich.With<ThreadEnricher>()
                .Enrich.With<FlowContextEnricher>()
                .MinimumLevel.Debug()
                .WriteTo.Airlock(LogEventLevel.Information);
            if (hostingEnvironment.Log != null)
                loggerConfiguration = loggerConfiguration.WriteTo.VostokLog(hostingEnvironment.Log);
            var logger = loggerConfiguration.CreateLogger();
            return new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://*:{hostingEnvironment.Configuration["port"]}/")
                .AddVostokServices()
				.ConfigureServices(s => ConfigureServices(s, hostingEnvironment, logger))
                .UseSerilog(logger)
                .Configure(app =>
                {
                    var env = app.ApplicationServices.GetRequiredService<IHostingEnvironment>();
                    app.UseVostok();
                    if (env.IsDevelopment())
                        app.UseDeveloperExceptionPage();
					
                    app.UseMvc();
					app.UseAuthentication();
					
					var database = app.ApplicationServices.GetService<UlearnDb>();
					database.MigrateToLatestVersion();
					var initialDataCreator = app.ApplicationServices.GetService<InitialDataCreator>();
					database.CreateInitialDataAsync(initialDataCreator);
                })
                .Build();
        }

		private void ConfigureServices(IServiceCollection services, IVostokHostingEnvironment hostingEnvironment, Logger logger)
		{
			services.AddDbContext<UlearnDb>(
				options => options.UseSqlServer(hostingEnvironment.Configuration["database"])
			);
			
			/* DI */
			services.AddSingleton<ILogger>(logger);
			services.AddSingleton<ULearnUserManager>();
			services.AddSingleton<InitialDataCreator>();
			
			/* DI for database repos */
			services.AddScoped<UsersRepo>();
			services.AddScoped<CommentsRepo>();
			services.AddScoped<UserRolesRepo>();
			
			/* Asp.NET Core MVC */
			services.AddMvc();

			ConfigureAuthServices(services, hostingEnvironment, logger);
		}

		private void ConfigureAuthServices(IServiceCollection services, IVostokHostingEnvironment hostingEnvironment, Logger logger)
		{
			services.AddIdentity<ApplicationUser, IdentityRole>()
				.AddEntityFrameworkStores<UlearnDb>()
				.AddDefaultTokenProviders();

			services.ConfigureApplicationCookie(options =>
			{
				options.LoginPath = "/Account/Login";
				options.LogoutPath = "/Account/Logout";
			});

			services.AddAuthentication();
				//.AddVk()
		}
	}
}