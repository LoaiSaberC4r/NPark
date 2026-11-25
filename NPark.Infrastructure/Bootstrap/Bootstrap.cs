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
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NPark.Application.Abstraction;
using NPark.Application.Abstraction.Security;
using NPark.Application.Options;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Infrastructure.Authorization;
using NPark.Infrastructure.Options;
using NPark.Infrastructure.Services;
using NPark.Infrastructure.Services.Camera;
using NPark.Infrastructure.Services.Lpr;
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
            services.AddOptions<GateOperationOptions>().Bind(configuration.GetSection("GateOperation"))
                .Validate(x => x.RequireAcknowledgment && !x.Retain && x.CommandQos > 0 && x.CommandQos <= 2
                    && x.AckQos > 0 && x.AckQos <= 2 && x.AckTimeoutSeconds > 0 && x.RecoveryIntervalSeconds > 0
                    && x.DispatchWaitTimeoutSeconds > 0
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
            services.AddOptions<MqttOptions>().Bind(configuration.GetSection("Mqtt"))
                .Validate(x => !string.IsNullOrWhiteSpace(x.Host) && x.Port is > 0 and <= 65535 &&
                    !string.IsNullOrWhiteSpace(x.ClientId) && x.KeepAliveSeconds > 0 && x.ReconnectSeconds > 0,
                    "Configure a valid MQTT host, port, client ID, keep-alive and reconnect interval.")
                .ValidateOnStart();
            services.Configure<MqttHeartbeatOptions>(configuration.GetSection("MqttHeartbeat"));
            services.AddHostedService<MqttNetworkHeartbeatService>();

            services.AddSingleton<IMqttPublisher, MqttPublisher>();
            services.AddEntranceIntegration(configuration);
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

        private static void AddEntranceIntegration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<LprCaptureOptions>().Bind(configuration.GetSection("LprCapture"))
                .Validate(x => x.RetentionDays is >= 1 and <= 365 && x.ArchiveCheckIntervalMinutes is >= 1 and <= 1440
                    && x.ArchiveBatchSize is >= 1 and <= 5000 && (x.MaxMatchingAgeSeconds is null or > 0),
                    "Invalid LPR capture retention, batch, interval or matching age.").ValidateOnStart();
            services.AddOptions<MqttParkingEventsOptions>().Bind(configuration.GetSection("MqttParkingEvents"))
                .Validate(x => x.SubscriptionQos is 0 or 1, "Invalid MQTT parking event subscription QoS.").ValidateOnStart();
            services.AddOptions<SubscriberButtonLockOptions>().Bind(configuration.GetSection("SubscriberButtonLock"))
                .Validate<IOptions<LprCaptureOptions>, IOptions<GateOperationOptions>>((x, lpr, gates) =>
                    !x.Enabled || lpr.Value.Enabled && ValidEntranceCommandSettings(gates.Value, x.SupportedControllerIds)
                        && x.DuplicateWindowSeconds is >= 1 and <= 60,
                    "Subscriber button lock requires LPR persistence, supported controller IDs and the v1.1.11 MQTT command settings.")
                .ValidateOnStart();
            services.AddOptions<AutomaticVisitorEntryOptions>().Bind(configuration.GetSection("AutomaticVisitorEntry"))
                .Validate<IOptions<MqttParkingEventsOptions>, IOptions<GateOperationOptions>>((x, mqtt, gates) =>
                    !x.Enabled || mqtt.Value.Enabled && x.SystemUserId.HasValue && x.SystemUserId != Guid.Empty &&
                        ValidEntranceCommandSettings(gates.Value, x.SupportedControllerIds),
                    "Automatic visitor entry requires MQTT event intake, a system user ID, supported controller IDs and the v1.1.11 MQTT command settings.")
                .ValidateOnStart();
            services.AddSingleton<ParkingEventStorage>();
            services.AddHostedService<LprCaptureArchiveService>();
            services.AddHostedService<MqttParkingEventSubscriber>();
            services.AddHostedService<ParkingEventProcessingService>();
        }

        private static bool ValidEntranceCommandSettings(GateOperationOptions gates, int[] controllers) =>
            controllers.Length > 0 && controllers.All(id => id is >= 1 and <= ushort.MaxValue) &&
            gates is { CommandTopicTemplate: "sub/network/{controllerId}", CommandQos: 1, Retain: false };

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

        private sealed class SeedingHostedService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<SeedingHostedService> logger) : IHostedLifecycleService
        {
            private const string ApplyMigrationsKey = "Database:ApplyMigrationsOnStartup";

            // The host finishes StartingAsync for all services before calling any StartAsync,
            // including when hosted services start concurrently.
            public async Task StartingAsync(CancellationToken cancellationToken)
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var db = scope.ServiceProvider.GetRequiredService<NParkDBContext>();
                    var applyMigrations = configuration.GetValue(ApplyMigrationsKey, false);
                    var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();

                    if (pending.Length == 0)
                    {
                        logger.LogInformation("The NPark database schema is up to date.");
                    }
                    else
                    {
                        logger.LogInformation("Found {MigrationCount} pending NPark migrations: {Migrations}",
                            pending.Length, string.Join(", ", pending));

                        if (!applyMigrations)
                        {
                            logger.LogWarning("Automatic NPark database migrations are disabled.");
                            throw new InvalidOperationException(
                                "Pending NPark database migrations exist and automatic migrations are disabled. " +
                                $"Apply the migrations manually or enable {ApplyMigrationsKey}.");
                        }

                        logger.LogInformation("Applying NPark database migrations.");
                        await db.Database.MigrateAsync(cancellationToken);

                        var remaining = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
                        if (remaining.Length > 0)
                        {
                            throw new InvalidOperationException(
                                $"Pending NPark database migrations remain after migration: {string.Join(", ", remaining)}.");
                        }

                        logger.LogInformation("NPark database migrations completed successfully.");
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    await ReleaseHistoricalReservationsAsync(db, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    logger.LogWarning("NPark database initialization was canceled.");
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "NPark database initialization failed.");
                    throw;
                }

                var seeder = scope.ServiceProvider.GetRequiredService<IEnsureSeeding>();
                logger.LogInformation("Starting database seeding (HostedService).");
                await seeder.SeedDatabaseAsync();
                cancellationToken.ThrowIfCancellationRequested();
                logger.LogInformation("Database seeding completed (HostedService).");
            }

            private async Task ReleaseHistoricalReservationsAsync(NParkDBContext db, CancellationToken ct)
            {
                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                var operations = db.Set<GateOperation>();
                var terminal = await operations.Where(x => (x.ReservedControllerId != null || x.ReservedTicketId != null) &&
                        (x.Status == GateOperationStatus.Completed || x.Status == GateOperationStatus.Failed ||
                         x.Status == GateOperationStatus.TimedOut || x.Status == GateOperationStatus.Unknown))
                    .ExecuteUpdateAsync(set => set.SetProperty(x => x.ReservedControllerId, (int?)null)
                        .SetProperty(x => x.ReservedTicketId, (Guid?)null), ct);
                var acknowledged = await operations.Where(x => x.Status == GateOperationStatus.Acknowledged &&
                        x.AckStatusCode == 0 && x.ReservedControllerId != null)
                    .ExecuteUpdateAsync(set => set.SetProperty(x => x.ReservedControllerId, (int?)null), ct);
                await transaction.CommitAsync(ct);
                if (terminal > 0 || acknowledged > 0)
                    logger.LogInformation("Historical gate reservations released. TerminalOperations={TerminalOperations}, " +
                        "AcknowledgedControllers={AcknowledgedControllers}", terminal, acknowledged);
            }

            public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
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
