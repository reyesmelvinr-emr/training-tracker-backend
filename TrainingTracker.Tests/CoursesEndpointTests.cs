using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TrainingTracker.Tests;

public class CoursesEndpointTests : InMemoryTestBase
{
    public CoursesEndpointTests(InMemoryWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task List_courses_returns_seeded_items()
    {
        var client = Factory.CreateClient();
        var response = await client.GetAsync("/api/courses?page=1&pageSize=5");
        
        // Log response body if not OK to help diagnose CI failures
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected 200 OK but got {response.StatusCode}. Response body: {errorBody}");
        }
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.GetProperty("items");
        items.GetArrayLength().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("page").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Get_course_by_id_returns_200_when_exists()
    {
        var client = Factory.CreateClient();
        // First list to obtain an id
        var list = await client.GetStringAsync("/api/courses");
        using var listDoc = JsonDocument.Parse(list);
        var firstId = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetString();
        firstId.Should().NotBeNull();
        var detailResponse = await client.GetAsync($"/api/courses/{firstId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_course_by_id_returns_404_for_unknown()
    {
        var client = Factory.CreateClient();
        var response = await client.GetAsync($"/api/courses/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}