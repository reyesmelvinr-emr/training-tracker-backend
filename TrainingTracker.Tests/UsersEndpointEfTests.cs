using System.Net;
using FluentAssertions;

namespace TrainingTracker.Tests;

[Collection("EfDbCollection")]
public class UsersEndpointEfTests
{
    private readonly EfDatabaseFixture _fixture;
    public UsersEndpointEfTests(EfDatabaseFixture fixture) { _fixture = fixture; }

    [Fact]
    public async Task List_users_returns_ok_in_ef_mode()
    {
        var client = _fixture.Factory.CreateClient();
        var resp = await client.GetAsync("/api/users");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
