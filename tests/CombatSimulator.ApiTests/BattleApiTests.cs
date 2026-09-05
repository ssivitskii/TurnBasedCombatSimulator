using CombatSimulator.Api;
using CombatSimulator.Api.Controllers;
using CombatSimulator.Application;
using CombatSimulator.Application.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace CombatSimulator.ApiTests;

public sealed class BattleApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BattleApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RunReturnsReplayAndIsDeterministicForSameSeed()
    {
        using HttpClient client = _factory.CreateClient();
        object request = ValidRequest();
        using HttpResponseMessage first = await client.PostAsJsonAsync("/api/battles/run", request);
        using HttpResponseMessage second = await client.PostAsJsonAsync("/api/battles/run", request);
        string firstJson = await first.Content.ReadAsStringAsync();
        string secondJson = await second.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(firstJson, secondJson);
        using var json = JsonDocument.Parse(firstJson);
        Assert.NotEmpty(json.RootElement.GetProperty("events").EnumerateArray());
        Assert.Equal("teamDefeated", json.RootElement.GetProperty("result").GetProperty("endReason").GetString());
    }

    [Theory]
    [InlineData("{\"seed\":1,\"configuration\":{\"roundLimit\":10,\"teamA\":[{\"creature\":\"Unknown\"}],\"teamB\":[{\"creature\":\"MimicChest\"}]}}")]
    [InlineData("{\"seed\":1,\"configuration\":{\"roundLimit\":10,\"teamA\":[{\"creature\":\"MimicChest\",\"modifiers\":[\"Unknown\"]}],\"teamB\":[{\"creature\":\"MimicChest\"}]}}")]
    [InlineData("{\"seed\":1,\"configuration\":{\"roundLimit\":0,\"teamA\":[{\"creature\":\"MimicChest\"}],\"teamB\":[{\"creature\":\"MimicChest\"}]}}")]
    [InlineData("{\"seed\":1,\"configuration\":{\"roundLimit\":1001,\"teamA\":[{\"creature\":\"MimicChest\"}],\"teamB\":[{\"creature\":\"MimicChest\"}]}}")]
    [InlineData("{\"seed\":1,\"configuration\":{\"roundLimit\":10,\"teamA\":[],\"teamB\":[{\"creature\":\"MimicChest\"}]}}")]
    [InlineData("{\"seed\":1,\"configuration\":{\"roundLimit\":10,\"teamA\":[{\"creature\":\"MimicChest\",\"attack\":-1}],\"teamB\":[{\"creature\":\"MimicChest\"}]}}")]
    public async Task InvalidConfigurationsReturnProblemJson(string body)
    {
        using HttpClient client = _factory.CreateClient();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync("/api/battles/run", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    public async Task MalformedOrNullBodyReturnsProblemJson(string body)
    {
        using HttpClient client = _factory.CreateClient();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync("/api/battles/run", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public void LimiterConfigurationExposesFixedConcurrencyPolicy()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        RateLimiterOptions options = scope.ServiceProvider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

        Assert.Equal(StatusCodes.Status429TooManyRequests, options.RejectionStatusCode);
        Assert.NotNull(options);
        Assert.Equal(4, ApiLimits.MaximumConcurrentSimulations);
        Assert.Equal(0, ApiLimits.MaximumQueueLength);
        EnableRateLimitingAttribute attribute = Assert.Single(
            typeof(BattlesController).GetMethod(nameof(BattlesController.Run))!
                .GetCustomAttributes<EnableRateLimitingAttribute>());
        Assert.Equal(SimulationRateLimitingExtensions.PolicyName, attribute.PolicyName);
    }

    [Fact]
    public async Task TeamLargerThanSevenReturnsBadRequest()
    {
        using HttpClient client = _factory.CreateClient();
        var fighter = new { creature = "MimicChest", modifiers = Array.Empty<string>() };
        var request = new
        {
            seed = 42,
            configuration = new
            {
                roundLimit = 10,
                teamA = Enumerable.Repeat(fighter, 8).ToArray(),
                teamB = new[] { fighter },
            },
        };

        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/battles/run", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OversizedBodyReturnsPayloadTooLarge()
    {
        using HttpClient client = _factory.CreateClient();
        using var content = new StringContent(
            JsonSerializer.Serialize(new { padding = new string('x', (int)ApiLimits.MaximumRequestBytes) }),
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response = await client.PostAsync("/api/battles/run", content);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task MaximumOverridesRemainValidAcrossStatGrowth()
    {
        using HttpClient client = _factory.CreateClient();
        var request = new
        {
            seed = 42,
            configuration = new
            {
                roundLimit = 1,
                teamA = new[] { new { creature = "BattleAnalyst", attack = int.MaxValue, health = int.MaxValue } },
                teamB = new[] { new { creature = "ViciousBrawler", attack = int.MaxValue, health = int.MaxValue } },
            },
        };

        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/battles/run", request);
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains($"\"attack\":{int.MaxValue}", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnsupportedMediaTypeReturnsProblemJson()
    {
        using HttpClient client = _factory.CreateClient();
        using var content = new StringContent("<battle />", Encoding.UTF8, "application/xml");

        using HttpResponseMessage response = await client.PostAsync("/api/battles/run", content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("[\"MagicShield\",\"magicshield\"]")]
    [InlineData("[\"MagicShield\",\"DoubleStrike\",\"MagicShield\"]")]
    public async Task DuplicateOrExcessiveModifiersReturnProblemJson(string modifiers)
    {
        using HttpClient client = _factory.CreateClient();
        string body = $$"""
            {
              "seed": 42,
              "configuration": {
                "roundLimit": 10,
                "teamA": [{ "creature": "MimicChest", "modifiers": {{modifiers}} }],
                "teamB": [{ "creature": "AmuletMaster" }]
              }
            }
            """;
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PostAsync("/api/battles/run", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public void ApplicationServiceHonorsCancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        BattleConfiguration configuration = ValidConfiguration();

        Assert.Throws<OperationCanceledException>(() =>
            new BattleSimulationService().Run(configuration, 42, source.Token));
    }

    private static object ValidRequest() => new { seed = 42, configuration = ValidConfiguration() };

    private static BattleConfiguration ValidConfiguration() => new()
    {
        RoundLimit = 100,
        TeamA = [new BattleConfiguration.CreatureConfiguration { Creature = "AmuletMaster" }],
        TeamB = [new BattleConfiguration.CreatureConfiguration { Creature = "ViciousBrawler" }],
    };
}
