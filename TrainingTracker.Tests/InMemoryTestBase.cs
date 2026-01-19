using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TrainingTracker.Tests;

/// <summary>
/// Custom WebApplicationFactory that forces InMemory persistence mode.
/// </summary>
public class InMemoryWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to ensure predictable configuration loading
        builder.UseEnvironment("Testing");
        
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Clear all sources to ensure our config takes precedence
            config.Sources.Clear();
            
            // Add minimal configuration with InMemory mode
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Mode"] = "InMemory",
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
                ["AllowedHosts"] = "*"
            });
        });
    }
}

/// <summary>
/// Base class for tests that use in-memory repositories.
/// Ensures Persistence:Mode is set to "InMemory" regardless of appsettings files.
/// </summary>
public abstract class InMemoryTestBase : IClassFixture<InMemoryWebApplicationFactory>
{
    protected readonly WebApplicationFactory<Program> Factory;

    protected InMemoryTestBase(InMemoryWebApplicationFactory factory)
    {
        Factory = factory;
    }
}
