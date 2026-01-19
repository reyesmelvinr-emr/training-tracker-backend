using System.Net;
using FluentAssertions;

namespace TrainingTracker.Tests;

[Collection("EfDbCollection")]
public class EnrollmentsEndpointEfTests
{
    private readonly EfDatabaseFixture _fixture;
    public EnrollmentsEndpointEfTests(EfDatabaseFixture fixture) { _fixture = fixture; }

    [Fact]
    public async Task List_enrollments_returns_ok_in_ef_mode()
    {
        var client = _fixture.Factory.CreateClient();
        var resp = await client.GetAsync("/api/enrollments");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
