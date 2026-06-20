using FCG_CATALOG_API.Application.DTOs;
using FCG_CATALOG_API.Tests.Integration.Config;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace FCG_CATALOG_API.Tests.Integration.Games;

public class GameManagementIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private const string BaseRoute = "/api/v1/games";

    public GameManagementIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static StringContent Json(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    // ---- POST /api/v1/games ----

    [Fact]
    public async Task CreateGame_AsAdmin_Returns201()
    {
        var client = _factory.CreateClientWithToken("Admin");
        var dto = new GameDto { Title = "Elden Ring", Description = "Action RPG", Price = 199.90m, Genre = "RPG" };

        var response = await client.PostAsync(BaseRoute, Json(dto));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateGame_AsAdmin_ReturnedBodyContainsTitle()
    {
        var client = _factory.CreateClientWithToken("Admin");
        var dto = new GameDto { Title = "God of War", Description = "Action", Price = 149.99m, Genre = "Action" };

        var response = await client.PostAsync(BaseRoute, Json(dto));
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("God of War");
    }

    [Fact]
    public async Task CreateGame_AsUser_Returns403()
    {
        var client = _factory.CreateClientWithToken("User");
        var dto = new GameDto { Title = "Jogo Proibido", Price = 49.99m };

        var response = await client.PostAsync(BaseRoute, Json(dto));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateGame_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var dto = new GameDto { Title = "Jogo Sem Auth", Price = 49.99m };

        var response = await client.PostAsync(BaseRoute, Json(dto));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateGame_EmptyTitle_Returns400()
    {
        var client = _factory.CreateClientWithToken("Admin");
        var dto = new GameDto { Title = "", Price = 49.99m };

        var response = await client.PostAsync(BaseRoute, Json(dto));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- PATCH /api/v1/games/{id} ----

    [Fact]
    public async Task UpdateGame_AsAdmin_Returns200()
    {
        var game = await _factory.SeedGameAsync("Jogo para Atualizar");
        var client = _factory.CreateClientWithToken("Admin");
        var dto = new GameDto { Title = "Jogo Atualizado", Price = 29.99m, IsActive = true };

        var response = await client.PatchAsync($"{BaseRoute}/{game.Id}", Json(dto));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateGame_AsAdmin_ReturnedBodyContainsNewTitle()
    {
        var game = await _factory.SeedGameAsync("Jogo Original");
        var client = _factory.CreateClientWithToken("Admin");
        var dto = new GameDto { Title = "Jogo com Novo Título", Price = 59.99m, IsActive = true };

        var response = await client.PatchAsync($"{BaseRoute}/{game.Id}", Json(dto));
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Jogo com Novo Título");
    }

    [Fact]
    public async Task UpdateGame_AsUser_Returns403()
    {
        var game = await _factory.SeedGameAsync("Jogo que User não pode atualizar");
        var client = _factory.CreateClientWithToken("User");
        var dto = new GameDto { Title = "Tentativa Negada", Price = 9.99m };

        var response = await client.PatchAsync($"{BaseRoute}/{game.Id}", Json(dto));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateGame_NonExistentId_Returns400()
    {
        var client = _factory.CreateClientWithToken("Admin");
        var dto = new GameDto { Title = "Qualquer", Price = 9.99m };

        var response = await client.PatchAsync($"{BaseRoute}/{Guid.NewGuid()}", Json(dto));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- DELETE /api/v1/games/{id} ----

    [Fact]
    public async Task DeleteGame_AsAdmin_Returns200()
    {
        var game = await _factory.SeedGameAsync("Jogo para Deletar");
        var client = _factory.CreateClientWithToken("Admin");

        var response = await client.DeleteAsync($"{BaseRoute}/{game.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteGame_AsAdmin_ReturnedBodyContainsGameTitle()
    {
        var game = await _factory.SeedGameAsync("Jogo que Será Removido");
        var client = _factory.CreateClientWithToken("Admin");

        var response = await client.DeleteAsync($"{BaseRoute}/{game.Id}");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Jogo que Será Removido");
    }

    [Fact]
    public async Task DeleteGame_AsUser_Returns403()
    {
        var game = await _factory.SeedGameAsync("Jogo que User não pode remover");
        var client = _factory.CreateClientWithToken("User");

        var response = await client.DeleteAsync($"{BaseRoute}/{game.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteGame_NonExistentId_Returns400()
    {
        var client = _factory.CreateClientWithToken("Admin");

        var response = await client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- GET /api/v1/games ----

    [Fact]
    public async Task GetAllGames_AsUser_Returns200()
    {
        var client = _factory.CreateClientWithToken("User");

        var response = await client.GetAsync(BaseRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllGames_AsAdmin_Returns200()
    {
        var client = _factory.CreateClientWithToken("Admin");

        var response = await client.GetAsync(BaseRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllGames_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(BaseRoute);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
