using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TrainingTracker.Tests;

public class EnrollmentsEndpointTests : InMemoryTestBase
{
    public EnrollmentsEndpointTests(InMemoryWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task List_enrollments_returns_ok()
    {
        var client = Factory.CreateClient();
        var resp = await client.GetAsync("/api/enrollments");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
