using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace TrainingTracker.Tests;

[Collection("EfDbCollection")]
public class CoursesEndpointEfTests
{
    private readonly EfDatabaseFixture _fixture;

    public CoursesEndpointEfTests(EfDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task List_courses_returns_ok_in_ef_mode()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync("/api/courses?page=1&pageSize=5");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_courses_returns_404_for_unknown_in_ef_mode()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync($"/api/courses/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}