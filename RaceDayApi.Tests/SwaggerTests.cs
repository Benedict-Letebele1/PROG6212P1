using System.Net;
using System.Text.Json;
using RaceDayApi.Tests.Infrastructure;

namespace RaceDayApi.Tests;

/// <summary>Swagger must describe every endpoint so the plan can be checked in a browser.</summary>
public class SwaggerTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task SwaggerDocument_DescribesTheMainEndpoints()
    {
        var response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/auth/login", out _));
        Assert.True(paths.TryGetProperty("/api/events", out _));
        Assert.True(paths.TryGetProperty("/api/events/{eventId}/enrol", out _));
        Assert.True(paths.TryGetProperty("/api/events/{eventId}/results", out _));
    }

    [Fact]
    public async Task SwaggerUi_IsServed()
    {
        var response = await factory.CreateClient().GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
