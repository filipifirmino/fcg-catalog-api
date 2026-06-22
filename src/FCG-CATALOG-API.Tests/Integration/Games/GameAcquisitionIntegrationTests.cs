using FCG_CATALOG_API.Tests.Integration.Config;
using FluentAssertions;
using System.Net;

namespace FCG_CATALOG_API.Tests.Integration.Games;

public class GameAcquisitionIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private const string BaseRoute = "/api/v1/games";

    public GameAcquisitionIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---- POST /api/v1/games/{id}/acquire ----

    [Fact]
    public async Task AcquireGame_AsUser_Returns202Accepted()
    {
        var game = await _factory.SeedGameAsync("Jogo para Adquirir");
        var client = _factory.CreateClientWithToken("User");

        var response = await client.PostAsync($"{BaseRoute}/{game.Id}/acquire", null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task AcquireGame_AsAdmin_Returns202Accepted()
    {
        var game = await _factory.SeedGameAsync("Jogo para Adquirir Admin");
        var client = _factory.CreateClientWithToken("Admin");

        var response = await client.PostAsync($"{BaseRoute}/{game.Id}/acquire", null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task AcquireGame_Unauthenticated_Returns401()
    {
        var game = await _factory.SeedGameAsync("Jogo Sem Auth");
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"{BaseRoute}/{game.Id}/acquire", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AcquireGame_NonExistentGame_Returns400()
    {
        var client = _factory.CreateClientWithToken("User");

        var response = await client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/acquire", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AcquireGame_AlreadyOwned_Returns400()
    {
        var game = await _factory.SeedGameAsync("Jogo já Adquirido");
        // The controller currently uses Guid.Empty as userId, so we seed with Guid.Empty
        await _factory.SeedAcquisitionAsync(Guid.Empty, game.Id, game.Price);
        var client = _factory.CreateClientWithToken("User");

        var response = await client.PostAsync($"{BaseRoute}/{game.Id}/acquire", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AcquireGame_Returns202_WithSuccessBody()
    {
        var game = await _factory.SeedGameAsync("Jogo Body Test");
        var client = _factory.CreateClientWithToken("User");

        var response = await client.PostAsync($"{BaseRoute}/{game.Id}/acquire", null);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        body.Should().NotBeNullOrWhiteSpace();
    }
}
