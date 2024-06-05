using EClaimsEntities;
using EClaimsRepository.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EClaimsRepository.Repository;
using Hangfire;
using EClaimsWeb.Helpers;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace EClaimsWeb.Extensions
{
    public static class ServiceExtensions
    {
        public static void ConfigureCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
            });
        }

        public static void ConfigureIISIntegration(this IServiceCollection services)
        {
            services.Configure<IISOptions>(options =>
            {
            });
            services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = int.MaxValue; // if don't set default value is: 30 MB
            });
            services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = int.MaxValue;
            });
            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // if don't set default value is: 128 MB
                x.MultipartHeadersLengthLimit = int.MaxValue;
            });
        }

        public static void ConfigureLoggerService(this IServiceCollection services)
        {
            services.AddSingleton<ILoggerManager, LoggerManager>();
        }


        public static void ConfigureSqlServerContext(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<RepositoryContext>(opts =>
               opts.UseSqlServer(config.GetConnectionString("sqlConnection"),
               options => options.MigrationsAssembly("EClaimsWeb")),ServiceLifetime.Transient);
            services.AddScoped<IRepositoryContext>(provider => provider.GetService<RepositoryContext>());
            services.AddScoped<IApplicationWriteDbConnection, ApplicationWriteDbConnection>();
            services.AddScoped<IApplicationReadDbConnection, ApplicationReadDbConnection>();
        }

        public static void ConfigureHangFireContext(this IServiceCollection services, IConfiguration config)
        {
            //#region Configure Connection String
            //services.AddDbContext<RepositoryContext>(item => item.UseSqlServer(config.GetConnectionString("sqlConnection")));
            //#endregion
            #region Configure Hangfire
            services.AddHangfire(c => c.UseSqlServerStorage(config.GetConnectionString("sqlConnection"),new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                UseRecommendedIsolationLevel = true,
                PrepareSchemaIfNecessary = false, // Default value: true
                EnableHeavyMigrations = false,     // Default value: false
                DisableGlobalLocks = true
            }));
            GlobalConfiguration.Configuration.UseSqlServerStorage(config.GetConnectionString("sqlConnection")).WithJobExpirationTimeout(TimeSpan.FromDays(7));
            #endregion
            services.AddScoped<ISendMailServices, SendMailServices>();
        }




        public static void ConfigureRepositoryWrapper(this IServiceCollection services)
        {
            services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
        }
    }
}
