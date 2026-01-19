using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;

namespace TrainingTracker.Tests;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task GET_health_returns_ok_with_version_and_uptime_and_correlation_id()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("X-Correlation-Id").Should().BeTrue();
        var correlation = response.Headers.GetValues("X-Correlation-Id").First();
        correlation.Should().NotBeNullOrWhiteSpace();

        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotBeNullOrWhiteSpace();

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetString().Should().Be("ok");
        doc.RootElement.GetProperty("version").GetString().Should().NotBeNullOrWhiteSpace();
        doc.RootElement.GetProperty("uptimeSeconds").GetInt64().Should().BeGreaterOrEqualTo(0);
        doc.RootElement.GetProperty("timestampUtc").GetDateTime().Should().BeBefore(DateTime.UtcNow.AddSeconds(5));
    }
}
