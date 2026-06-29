using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Business.WebApi.Hubs;
using Osmos.Business.WebApi.Models;
using Osmos.Core.Data.NoSql.Models;
using Osmos.Workers.Helpers.Eth;
using Osmos.Workers.Helpers.Eth.Models;
using System.Text.Json.Serialization;

namespace Osmos.Business.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
            _appIdentityOptions = configuration.GetSection("AppIdentityOptions").Get<AppIdentityOptions>();

            IdentityServerConfig.Init();
        }

        private readonly IConfiguration _configuration = null;
        private readonly IWebHostEnvironment _environment = null;
        private readonly AppIdentityOptions _appIdentityOptions = null;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();

            services
                .AddMvcCore()
                .AddAuthorization();

            services
                .AddControllers(config =>
                {
                    var policy = new AuthorizationPolicyBuilder()
                                 .RequireAuthenticatedUser()
                                 .Build();
                    config.Filters.Add(new AuthorizeFilter(policy));
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            services.Configure<AppIdentityOptions>(_configuration.GetSection("AppIdentityOptions"));
            services.Configure<MongoDbOptions>(_configuration.GetSection("MongoDb"));
            services.Configure<EthSettings>(_configuration.GetSection("Eth"));
            services.AddScoped<EthService>();

            services.AddScoped(TokensManager.GetImplementationFactory());
            services.AddScoped(TxnsManager.GetImplementationFactory());
            services.AddScoped(HotTokensManager.GetImplementationFactory());
            services.AddScoped(DataInfosManager.GetImplementationFactory());
            services.AddScoped(TelegramOpportunityMessagesManager.GetImplementationFactory());
            services.AddScoped(TelegramScoreMessagesManager.GetImplementationFactory());
            services.AddScoped(PromotedTokensManager.GetImplementationFactory());
            services.AddScoped(TxnNotificationsManager.GetImplementationFactory());
            services.AddScoped(GameDataManager.GetImplementationFactory());
            services.AddScoped(BetsManager.GetImplementationFactory());

            services.AddAuthentication().AddCookie("dummy");

            var builder = services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                options.IssuerUri = _appIdentityOptions.IssuerUri;
                options.Authentication.CookieAuthenticationScheme = "dummy";
            })
                .AddTestUsers(IdentityServerConfig.Users);

            builder.AddInMemoryIdentityResources(IdentityServerConfig.GetIdentityResources());
            builder.AddInMemoryApiScopes(IdentityServerConfig.ApiScopes);
            builder.AddInMemoryApiResources(IdentityServerConfig.GetApiResources());
            builder.AddInMemoryClients(IdentityServerConfig.Clients);

            services
            .AddAuthentication("Bearer")
            .AddIdentityServerAuthentication(options =>
            {
                options.Authority = _appIdentityOptions.Authority;
                options.RequireHttpsMetadata = false;

                options.ApiName = "api";
                options.ApiSecret = "secret";

            });

            // not recommended for production - you need to store your key material somewhere secure
            builder.AddDeveloperSigningCredential();

            services.AddSignalR()
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.Converters
                       .Add(new JsonStringEnumConverter());
                });
            //services.AddCors(options =>
            //{
            //    options.AddDefaultPolicy(builder =>
            //    {
            //        builder.WithOrigins("https://example.com")
            //            .AllowCredentials();
            //    });
            //});
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseStaticFiles();

            app.UseCors(x => x
                .AllowAnyMethod()
                .AllowAnyHeader()
                .SetIsOriginAllowed(origin => true)
                .AllowCredentials()
                .WithExposedHeaders(new string[] { "Location", "os-page-count" }));

            app.UseIdentityServer();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<AppHub>("/appHub");
            });
        }
    }
}
