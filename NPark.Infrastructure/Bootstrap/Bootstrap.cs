using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Behaviors;
using NPark.Application.Services;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Infrastracture.Bootstrap;
using BuildingBlock.Infrastracture.Interceptors;
using BuildingBlock.Infrastracture.Options;
using BuildingBlock.Infrastracture.Repositories;
using BuildingBlock.Infrastracture.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NPark.Application.Abstraction;
using NPark.Application.Abstraction.Security;
using NPark.Application.Options;
using NPark.Infrastructure.Authorization;
using NPark.Infrastructure.Options;
using NPark.Infrastructure.Services;
using NPark.Infrastructure.Services.Camera;
using NPark.Infrastructure.Services.Mqtt;
using NPark.Infrastructure.Services.Seeders;
using NPark.Infrastructure.Services.Token;
using System.Text;

namespace NPark.Infrastructure.Bootstrap
{
    public static class Bootstrap
    {
        public static IServiceCollection InfrastructureInjection(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<EncryptionOptions>(configuration.GetSection("EncryptionOptions"));
            services.Configure<SalaryConfig>(configuration.GetSection("SalaryConfig"));
            services.AddDbConfig(configuration);
            services.AddScoped<IDbContextProvider, DbContextProvider<NParkDBContext>>();
            services.AddScoped<IAuditLogger, AuditLogger>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddSecurity();
            services.AddEfInfrastructure();
            services.AddBuildingBlockAuditingAndSoftDelete();

            services.AddHttpClient();
            services.AddSingleton<ICacheService, MemoryCacheService>();
            services.AddSingleton<IByteVerificationService, ByteVerificationService>();
            services.AddMediaService();
            services.AddQrCodeService();
            services.AddSeeding();
            services.AddAuth(configuration);
            services.AddHttpClient("device-http", c =>
            {
            }).SetHandlerLifetime(TimeSpan.FromMinutes(5));

            services.AddScoped<ISendProtocol, SendProtocol>();
            services.AddScoped<ITokenReader, JwtReader>();

            services.AddSingleton(TimeProvider.System);
            services.AddOptions<ParkingBusinessOptions>().Bind(configuration.GetSection("ParkingBusiness"))
                .Validate(x => TimeZoneInfo.TryFindSystemTimeZoneById(x.TimeZoneId, out _), "Invalid parking business time zone")
                .ValidateOnStart();
            services.AddOptions<OvernightParkingOptions>().Bind(configuration.GetSection("OvernightParking"))
                .Validate(x => Enum.IsDefined(x.PenaltyMode) && x.RequireFullPenaltyPayment, "Invalid overnight payment policy")
                .ValidateOnStart();
            services.AddOptions<GateOperationOptions>().Bind(configuration.GetSection("GateOperation"))
                .Validate(x => x.RequireAcknowledgment && !x.Retain && x.CommandQos > 0 && x.CommandQos <= 2
                    && x.AckQos > 0 && x.AckQos <= 2 && x.AckTimeoutSeconds > 0 && x.RecoveryIntervalSeconds > 0
                    && x.MaxRetries >= 0 && x.RetryIntervalMilliseconds >= 0
                    && (!x.EnableAutomaticRetry || x.HardwareDeduplicationConfirmed)
                    && x.CommandTopicTemplate.Split("{controllerId}").Length == 2
                    && !x.CommandTopicTemplate.Contains('+') && !x.CommandTopicTemplate.Contains('#')
                    && (x.AckTopicFilter == "ack/network/+" || x.AckTopicFilter == "ack/network/#"),
                    "Invalid acknowledged gate configuration")
                .ValidateOnStart();
            services.AddScoped<IVerifyTransactionCommit, TransactionCommitVerifier>();
            services.AddSingleton<GateAckSubscriptionState>();
            services.AddSingleton<GateOperationRecoveryService>();
            services.AddSingleton<IGateOperationDispatcher>(sp => sp.GetRequiredService<GateOperationRecoveryService>());
            services.AddHostedService(sp => sp.GetRequiredService<GateOperationRecoveryService>());
            services.AddHostedService<GateAckSubscriber>();
            services.Configure<MqttOptions>(configuration.GetSection("Mqtt"));
            services.Configure<MqttHeartbeatOptions>(configuration.GetSection("MqttHeartbeat"));
            services.AddHostedService<MqttNetworkHeartbeatService>();

            services.AddSingleton<IMqttPublisher, MqttPublisher>();
            services.Configure<CameraOptions>(configuration.GetSection("Camera"));

            services.AddHttpClient<ICameraSnapshotService, OnvifCameraSnapshotService>((sp, client) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CameraOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds <= 0 ? 15 : options.TimeoutSeconds);
            });
            services.Configure<MqttNetworkSubscriberOptions>(
    configuration.GetSection(MqttNetworkSubscriberOptions.SectionName));
            services.AddHostedService<MqttNetworkSubscriberService>();
            services.AddSingleton<IMqttExitQrPacketDecoder, MqttExitQrPacketDecoder>();
            return services;
        }

        private static IServiceCollection AddDbConfig(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("Database")!;

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("A valid database connection string must be provided.");

            services.AddDbContext<NParkDBContext>((sp, options) =>
            {
                options.UseSqlServer(connectionString);
                options.AddInterceptors(sp.GetRequiredService<SoftDeleteEntitiesInterceptor>());
                options.AddInterceptors(sp.GetRequiredService<AuditableEntitiesInterceptor>());
                options.AddInterceptors(sp.GetRequiredService<DomainEventsInterceptor>());
            });

            return services;
        }

        #region Seeding Database

        private static IServiceCollection AddSeeding(this IServiceCollection services)
        {
            services.AddScoped<ISeeder, PermissionSeeder>();
            services.AddScoped<ISeeder, RoleSeeder>();
            services.AddScoped<ISeeder, RolePermissionSeeder>();
            services.AddScoped<ISeeder, IUserSeeder>();
            services.AddScoped<IEnsureSeeding, EnsureSeeding>();
            services.AddHostedService<SeedingHostedService>();

            return services;
        }

        private sealed class SeedingHostedService : IHostedService
        {
            private readonly IServiceScopeFactory _scopeFactory;
            private readonly ILogger<SeedingHostedService> _logger;

            public SeedingHostedService(IServiceScopeFactory scopeFactory, ILogger<SeedingHostedService> logger)
            {
                _scopeFactory = scopeFactory;
                _logger = logger;
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NParkDBContext>();

                if ((await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
                    throw new InvalidOperationException("Apply the reviewed NPark database migrations before starting the API.");

                var seeder = scope.ServiceProvider.GetRequiredService<IEnsureSeeding>();
                _logger.LogInformation("Starting database seeding (HostedService)...");
                await seeder.SeedDatabaseAsync();
                _logger.LogInformation("Database seeding completed (HostedService).");
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        #endregion Seeding Database

        #region Authentication & Authorization

        private static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IJwtProvider, JwtProvider>();
            var jwtSettings = new JwtOption();
            configuration.GetSection(JwtOption.SectionName).Bind(jwtSettings);

            services.Configure<JwtOption>(
                configuration.GetSection(JwtOption.SectionName));
            services.AddAuthentication(defaultScheme: JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options =>
                        {
                            options.Events = new JwtBearerEvents
                            {
                                OnMessageReceived = context =>
                                {
                                    if (context.HttpContext.Request.Path.StartsWithSegments("/hubs/notifications") &&
                                        string.IsNullOrEmpty(context.Request.Headers.Authorization))
                                        context.Token = context.Request.Query["access_token"].FirstOrDefault();
                                    return Task.CompletedTask;
                                }
                            };
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuer = true,
                                ValidateAudience = true,
                                ValidateLifetime = true,
                                ValidateIssuerSigningKey = true,
                                ValidIssuer = jwtSettings.Issuer,
                                ValidAudience = jwtSettings.Audience,
                                IssuerSigningKey = new SymmetricSecurityKey(
                                    Encoding.UTF8.GetBytes(jwtSettings.Secret))
                            };
                        });

            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            return services;
        }

        #endregion Authentication & Authorization
    }
}