using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TrainingTracker.Tests;

public class UsersEndpointTests : InMemoryTestBase
{
    public UsersEndpointTests(InMemoryWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task List_users_returns_ok()
    {
        var client = Factory.CreateClient();
        var resp = await client.GetAsync("/api/users");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
