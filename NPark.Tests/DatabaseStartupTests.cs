using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction;
using NPark.Application.Bootstrap;
using NPark.Application.Feature.MqttNetworkManagement;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Infrastructure;
using NPark.Infrastructure.Bootstrap;
using Xunit;

namespace NPark.Tests;

[CollectionDefinition("Database startup configuration", DisableParallelization = true)]
public sealed class DatabaseStartupConfigurationCollection;

[Collection("Database startup configuration")]
public sealed class DatabaseStartupTests
{
    private const string Setting = "Database:ApplyMigrationsOnStartup";
    private const string EnvironmentSetting = "Database__ApplyMigrationsOnStartup";
    private const string Baseline = "20260505150257_AddingSuperVisorRecord";
    private const string Latest = "20261124122834_ParkingFinancialIntegrityV2";

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Testing", true)]
    [InlineData("Production", false)]
    [InlineData("Default", false)]
    public void AppSettingsProvideTheExpectedEnvironmentDefault(string environment, bool expected)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        Assert.Equal(expected, configuration.GetValue<bool>(Setting));
    }

    [SqlFact]
    public async Task EnabledStartupMigratesThenSeedsBeforeAnyWorkerStarts()
    {
        await using var database = await StartupDatabase.CreateAtBaselineAsync();
        var seedingBegan = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finishSeeding = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var probe = new StartupProbe(async db =>
        {
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.Contains(Latest, await db.Database.GetAppliedMigrationsAsync());
            seedingBegan.SetResult();
            await finishSeeding.Task;
        });
        using var host = CreateHost(database.ConnectionString, true, probe);
        var startup = host.StartAsync();
        try
        {
            var reached = await Task.WhenAny(startup, seedingBegan.Task).WaitAsync(TimeSpan.FromSeconds(30));
            await reached;
            Assert.True(seedingBegan.Task.IsCompletedSuccessfully);
            Assert.False(startup.IsCompleted);
            Assert.False(probe.WorkerStarted);
        }
        finally
        {
            finishSeeding.TrySetResult();
            await startup;
        }

        Assert.True(probe.WorkerStarted);
        Assert.Contains(probe.Logs, l => l.Message.Contains(Latest));
        Assert.Contains(probe.Logs, l => l.Message.Contains("migrations completed successfully"));
        await host.StopAsync();
    }

    [SqlFact]
    public async Task UpToDateDatabaseSeedsWithEitherSettingAndMigrationsAreNotReapplied()
    {
        await using var database = await StartupDatabase.CreateAtBaselineAsync();
        await using var db = database.Open();
        await db.Database.MigrateAsync();
        var originalHistory = (await db.Database.GetAppliedMigrationsAsync()).ToArray();

        foreach (var enabled in new[] { true, true, false })
        {
            var probe = new StartupProbe();
            using var host = CreateHost(database.ConnectionString, enabled, probe);
            await host.StartAsync();
            Assert.True(probe.SeedingFinished);
            Assert.True(probe.WorkerStarted);
            Assert.Contains(probe.Logs, l => l.Message.Contains("schema is up to date"));
            Assert.DoesNotContain(probe.Logs, l => l.Message.Contains("Applying NPark database migrations"));
            Assert.Equal(originalHistory, (await db.Database.GetAppliedMigrationsAsync()).ToArray());
            await host.StopAsync();
        }
    }

    [SqlFact]
    public async Task StartupReleasesHistoricalReservationsBeforeWorkersAndRepairsOnlyOnce()
    {
        await using var database = await StartupDatabase.CreateAtBaselineAsync();
        var now = DateTime.UtcNow.AddDays(1);
        var states = new[] { GateOperationStatus.TimedOut, GateOperationStatus.Unknown, GateOperationStatus.Failed,
            GateOperationStatus.Completed, GateOperationStatus.Acknowledged, GateOperationStatus.Pending };
        var operations = states.Select((_, i) => new GateOperation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            (ushort)(i + 1), GateOperationType.Exit, now)).ToArray();
        await using (var db = database.Open())
        {
            await db.Database.MigrateAsync();
            db.AddRange(operations);
            await db.SaveChangesAsync();
            for (var i = 0; i < operations.Length - 1; i++)
            {
                var ack = states[i] switch
                {
                    GateOperationStatus.Completed or GateOperationStatus.Acknowledged => (byte?)0,
                    GateOperationStatus.Failed => (byte?)1,
                    _ => null
                };
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE [GateOperations] SET [Status] = {(int)states[i]}, [PublishedAtUtc] = {now}, [ErrorCode] = 'LegacyEvidence',
                        [AcknowledgedAtUtc] = {(ack.HasValue ? now : (DateTime?)null)}, [AckStatusCode] = {ack}
                    WHERE [Id] = {operations[i].Id}
                    """);
            }
        }

        Dictionary<Guid, byte[]>? versions = null;
        for (var pass = 0; pass < 2; pass++)
        {
            var probe = new StartupProbe(async db =>
            {
                var current = await db.Set<GateOperation>().AsNoTracking().ToDictionaryAsync(x => x.Id);
                Assert.Equal(operations.Length, current.Count);
                for (var i = 0; i < operations.Length; i++)
                {
                    var original = operations[i];
                    var row = current[original.Id];
                    Assert.Equal((states[i], original.CommandId, original.ControllerId, original.GateId, original.TicketIdSnapshot),
                        (row.Status, row.CommandId, row.ControllerId, row.GateId, row.TicketIdSnapshot));
                    Assert.Equal(i == 5 ? original.ControllerId : (int?)null, row.ReservedControllerId);
                    Assert.Equal(i >= 4 ? original.TicketIdSnapshot : null, row.ReservedTicketId);
                    Assert.Equal(i < 5 ? now : (DateTime?)null, row.PublishedAtUtc);
                    Assert.Equal(i is 2 or 3 or 4 ? now : (DateTime?)null, row.AcknowledgedAtUtc);
                    Assert.Equal(i < 5 ? "LegacyEvidence" : null, row.ErrorCode);
                    Assert.Null(row.ReconciledAtUtc);
                    if (versions is not null)
                        Assert.Equal(versions[row.Id], row.RowVersion);
                }
                versions = current.ToDictionary(x => x.Key, x => x.Value.RowVersion);
            });
            using var host = CreateHost(database.ConnectionString, false, probe);
            await host.StartAsync();
            await host.StopAsync();
        }
    }

    [SqlFact]
    public async Task DisabledOrMissingSettingBlocksStartupAndSeedingWhenMigrationsArePending()
    {
        await using var database = await StartupDatabase.CreateAtBaselineAsync();

        foreach (var enabled in new bool?[] { false, null })
        {
            var probe = new StartupProbe();
            using var host = CreateHost(database.ConnectionString, enabled, probe);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());
            Assert.Contains(Setting, error.Message);
            Assert.Contains(probe.Logs, l => l.Message.Contains(Latest));
            Assert.Contains(probe.Logs, l => l.Level == LogLevel.Warning && l.Message.Contains("disabled"));
            Assert.False(probe.SeedingStarted);
            Assert.False(probe.WorkerStarted);
        }

        await using var db = database.Open();
        Assert.Equal(Latest, Assert.Single(await db.Database.GetPendingMigrationsAsync()));
    }

    [SqlFact]
    public async Task EnvironmentVariableOverridesDisabledSetting()
    {
        await using var database = await StartupDatabase.CreateAtBaselineAsync();
        var probe = new StartupProbe();
        var original = Environment.GetEnvironmentVariable(EnvironmentSetting);
        IHost host;
        try
        {
            Environment.SetEnvironmentVariable(EnvironmentSetting, "true");
            host = CreateHost(database.ConnectionString, false, probe, useEnvironmentVariables: true);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvironmentSetting, original);
        }

        using (host)
        {
            await host.StartAsync();
            await using var db = database.Open();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.True(probe.SeedingFinished);
            await host.StopAsync();
        }
    }

    [SqlFact]
    public async Task CancellationDuringMigrationStopsInitialization()
    {
        await using var database = await StartupDatabase.CreateAtBaselineAsync();
        using var cancellation = new CancellationTokenSource();
        var interceptor = new PauseMigrationCommand();
        var probe = new StartupProbe();
        using var host = CreateHost(database.ConnectionString, true, probe, interceptor);
        var startup = host.StartAsync(cancellation.Token);
        try
        {
            var reached = await Task.WhenAny(startup, interceptor.MigrationStarted.Task)
                .WaitAsync(TimeSpan.FromSeconds(30));
            await reached;
            Assert.True(interceptor.MigrationStarted.Task.IsCompletedSuccessfully);
        }
        finally
        {
            cancellation.Cancel();
        }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => startup);
        Assert.Contains(probe.Logs, l => l.Level == LogLevel.Warning && l.Message.Contains("was canceled"));
        Assert.False(probe.SeedingStarted);
        Assert.False(probe.WorkerStarted);
        await using var db = database.Open();
        Assert.Equal(Latest, Assert.Single(await db.Database.GetPendingMigrationsAsync()));
    }

    [Fact]
    public async Task DatabaseConnectivityFailureIsLoggedAndPreventsStartup()
    {
        const string unavailable = "Server=tcp:127.0.0.1,1;Database=NParkStartupTest_Unreachable;" +
            "Integrated Security=True;TrustServerCertificate=True;Connect Timeout=1;Connect Retry Count=0";
        var probe = new StartupProbe();
        using var host = CreateHost(unavailable, true, probe);

        var error = await Assert.ThrowsAsync<SqlException>(() => host.StartAsync());
        Assert.Contains(probe.Logs, l => l.Level == LogLevel.Error && ReferenceEquals(l.Exception, error));
        Assert.False(probe.SeedingStarted);
        Assert.False(probe.WorkerStarted);
    }

    internal static IHost CreateHost(string connectionString, bool? enabled, StartupProbe probe,
        DbCommandInterceptor? interceptor = null, bool useEnvironmentVariables = false)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = connectionString,
            ["Mqtt:Host"] = "127.0.0.1",
            ["Mqtt:Port"] = "1",
            ["Mqtt:ClientId"] = "npark-startup-test",
            ["Mqtt:UseTls"] = "false",
            ["MqttHeartbeat:Enabled"] = "false",
            ["MqttNetworkSubscriber:Enabled"] = "false"
        };
        if (enabled.HasValue)
            settings[Setting] = enabled.Value.ToString();
        builder.Configuration.AddInMemoryCollection(settings);
        if (useEnvironmentVariables)
            builder.Configuration.AddEnvironmentVariables();

        builder.Logging.SetMinimumLevel(LogLevel.Information).AddProvider(new StartupLoggerProvider(probe));
        builder.Services.Configure<HostOptions>(options => options.ServicesStartConcurrently = true);
        builder.Services.AddHostedService(_ => new WorkerProbe(probe));
        builder.Services.AddApplicationBootstrap();
        builder.Services.InfrastructureInjection(builder.Configuration);
        builder.Services.AddSingleton<IMqttCaptureSignalRNotifier, UnusedCaptureNotifier>();
        builder.Services.AddScoped<IEnsureSeeding>(services => new SeederProbe(
            services.GetRequiredService<NParkDBContext>(), probe));
        if (interceptor is not null)
            builder.Services.ConfigureDbContext<NParkDBContext>(options => options.AddInterceptors(interceptor));
        return builder.Build();
    }

    internal sealed class StartupProbe(Func<NParkDBContext, Task>? seed = null)
    {
        public ConcurrentQueue<StartupLog> Logs { get; } = new();
        public bool SeedingStarted { get; private set; }
        public bool SeedingFinished { get; private set; }
        public bool WorkerStarted { get; private set; }

        public async Task SeedAsync(NParkDBContext db)
        {
            SeedingStarted = true;
            if (seed is not null)
                await seed(db);
            SeedingFinished = true;
        }

        public void StartWorker()
        {
            WorkerStarted = true;
            Assert.True(SeedingFinished);
        }
    }

    internal sealed record StartupLog(LogLevel Level, string Message, Exception? Exception);

    private sealed class StartupLoggerProvider(StartupProbe probe) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) =>
            new StartupLogger(probe, categoryName.EndsWith("SeedingHostedService", StringComparison.Ordinal));

        public void Dispose() { }
    }

    private sealed class StartupLogger(StartupProbe probe, bool record) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => record;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (record)
                probe.Logs.Enqueue(new(logLevel, formatter(state, exception), exception));
        }
    }

    private sealed class SeederProbe(NParkDBContext db, StartupProbe probe) : IEnsureSeeding
    {
        public Task SeedDatabaseAsync() => probe.SeedAsync(db);
    }

    private sealed class WorkerProbe(StartupProbe probe) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            probe.StartWorker();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class UnusedCaptureNotifier : IMqttCaptureSignalRNotifier
    {
        public Task NotifyCaptureDetectedAsync(MqttCaptureDetectedResponse payload, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class PauseMigrationCommand : DbCommandInterceptor
    {
        public TaskCompletionSource MigrationStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("DROP INDEX [IX_TicketExceedTransactions_TicketIdSnapshot]",
                StringComparison.Ordinal))
            {
                MigrationStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            return result;
        }
    }

    private sealed class StartupDatabase : IAsyncDisposable
    {
        private readonly string name = "NParkStartupTest_" + Guid.NewGuid().ToString("N");
        private readonly string serverConnection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("NPARK_TEST_SQL")) { InitialCatalog = "master" }.ConnectionString;

        public string ConnectionString =>
            new SqlConnectionStringBuilder(serverConnection) { InitialCatalog = name }.ConnectionString;

        public NParkDBContext Open() =>
            new(new DbContextOptionsBuilder<NParkDBContext>().UseSqlServer(ConnectionString).Options);

        public static async Task<StartupDatabase> CreateAtBaselineAsync()
        {
            var database = new StartupDatabase();
            try
            {
                await using var db = database.Open();
                await db.Database.MigrateAsync(Baseline);
                return database;
            }
            catch
            {
                await database.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            // This name is generated by the fixture and never comes from the configured connection string.
            using var pool = new SqlConnection(ConnectionString);
            SqlConnection.ClearPool(pool);
            await using var connection = new SqlConnection(serverConnection);
            await connection.OpenAsync();
            await using var command = new SqlCommand($"DROP DATABASE IF EXISTS [{name}];", connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
