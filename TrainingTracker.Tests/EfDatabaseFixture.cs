using System.Diagnostics;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using TrainingTracker.Api;

namespace TrainingTracker.Tests;

/// <summary>
/// Creates a transient database (name pattern TT_Test_GUID) on local SQL Server, publishes the SSDT-generated schema (DACPAC),
/// and exposes a configured WebApplicationFactory in EF mode. Drops the DB on dispose.
/// Requires sqlpackage in PATH or accessible via environment.
/// </summary>
public class EfDatabaseFixture : IAsyncLifetime
{
    private const string BaseDbNamePrefix = "TT_Test_";
    private readonly string _server = "localhost"; // adjust if needed
    private readonly string _dacpacRelativePath = @"..\\..\\..\\..\\POC_SpecKitProj\\POC_SpecKitProj\\bin\\Release\\POC_SpecKitProj.dacpac"; // prefer Release build
    public string DatabaseName { get; } = BaseDbNamePrefix + Guid.NewGuid().ToString("N");
    private readonly string _saPassword = Environment.GetEnvironmentVariable("SQL_SA_PWD") ?? "YourStrong!Passw0rd";
    public string ConnectionString => $"Server={_server};Database={DatabaseName};User Id=sa;Password={_saPassword};TrustServerCertificate=True;"; // dev only

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Resolve DACPAC path (throws if not found with detailed error message)
        var dacpac = ResolveDacpacPath();

        PublishDacpac(dacpac, DatabaseName);

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                var dict = new Dictionary<string, string?>
                {
                    ["Persistence:Mode"] = "Ef",
                    ["ConnectionStrings:TrainingTracker"] = ConnectionString
                };
                cfg.AddInMemoryCollection(dict!);
            });
        });

        // Quick connectivity check
        using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            using var master = new SqlConnection($"Server={_server};Database=master;User Id=sa;Password={_saPassword};TrustServerCertificate=True;");
            await master.OpenAsync();
            using var cmd = master.CreateCommand();
            cmd.CommandText = $"IF DB_ID('{DatabaseName}') IS NOT NULL BEGIN ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{DatabaseName}]; END";
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // swallow; cleanup best-effort
        }
    }

    private string ResolveDacpacPath()
    {
        // Try multiple paths to support both local dev and CI environments
        var solutionDirectory = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.Parent!.FullName;
        
        // Path candidates (ordered by priority)
        var candidates = new[]
        {
            // CI build structure: repo-root/POC_SpecKitProj/POC_SpecKitProj/bin/Release/POC_SpecKitProj.dacpac
            Path.Combine(solutionDirectory, "..", "POC_SpecKitProj", "POC_SpecKitProj", "bin", "Release", "POC_SpecKitProj.dacpac"),
            
            // Local dev relative path from backend folder
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _dacpacRelativePath)),
            
            // Alternative: Debug build
            Path.Combine(solutionDirectory, "..", "POC_SpecKitProj", "POC_SpecKitProj", "bin", "Debug", "POC_SpecKitProj.dacpac")
        };
        
        foreach (var candidate in candidates)
        {
            var normalized = Path.GetFullPath(candidate);
            if (File.Exists(normalized))
                return normalized;
        }
        
        // If none found, return the first candidate with error message
        throw new FileNotFoundException(
            $"DACPAC not found; build the SSDT project first. Searched paths:\n{string.Join("\n", candidates.Select(Path.GetFullPath))}");
    }

    private void PublishDacpac(string dacpacPath, string targetDb)
    {
        var args = $"/Action:Publish /SourceFile:\"{dacpacPath}\" /TargetServerName:{_server} /TargetDatabaseName:{targetDb} /TargetUser:sa /TargetPassword:{_saPassword} /p:BlockOnPossibleDataLoss=false /p:DropObjectsNotInSource=false";
        var psi = new ProcessStartInfo
        {
            FileName = "sqlpackage",
            Arguments = args,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start sqlpackage.");
        var outSb = new StringBuilder();
        proc.OutputDataReceived += (s, e) => { if (e.Data != null) outSb.AppendLine(e.Data); };
        proc.ErrorDataReceived += (s, e) => { if (e.Data != null) outSb.AppendLine(e.Data); };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.WaitForExit();
        if (proc.ExitCode != 0)
        {
            throw new Exception($"sqlpackage failed: {outSb.ToString()}");
        }
    }
}

// Collection definition to share fixture across EF tests
[CollectionDefinition("EfDbCollection")]
public class EfDbCollection : ICollectionFixture<EfDatabaseFixture> { }