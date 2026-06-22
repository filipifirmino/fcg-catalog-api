using FCG_CATALOG_API.Application.DTOs;
using FCG_CATALOG_API.Tests.Integration.Config;
using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace FCG_CATALOG_API.Tests.Integration.Flows;

public class GameStoreFlowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private const string BaseRoute = "/api/v1/games";

    public GameStoreFlowIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static StringContent Json(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    private static Guid ExtractIdFromBody(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var idStr = doc.RootElement
            .GetProperty("data")
            .GetProperty("id")
            .GetString();
        return Guid.Parse(idStr!);
    }

    [Fact]
    public async Task FullFlow_AdminCreatesGame_UserAcquires_Returns202()
    {
        var adminClient = _factory.CreateClientWithToken("Admin");
        var userClient = _factory.CreateClientWithToken("User");

        // Admin creates game
        var createBody = Json(new GameDto { Title = "Flow Test Game", Price = 59.99m, Genre = "Action" });
        var createResponse = await adminClient.PostAsync(BaseRoute, createBody);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var gameId = ExtractIdFromBody(createContent);

        // User acquires the game
        var acquireResponse = await userClient.PostAsync($"{BaseRoute}/{gameId}/acquire", null);
        acquireResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task FullFlow_AdminCreatesGame_CanBeFoundInListing()
    {
        var adminClient = _factory.CreateClientWithToken("Admin");
        var userClient = _factory.CreateClientWithToken("User");
        var uniqueTitle = $"Listable Game {Guid.NewGuid()}";

        // Admin creates game
        var createBody = Json(new GameDto { Title = uniqueTitle, Price = 29.99m, Genre = "Indie" });
        var createResponse = await adminClient.PostAsync(BaseRoute, createBody);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // User lists games and finds the created one
        var listResponse = await userClient.GetAsync(BaseRoute);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listContent = await listResponse.Content.ReadAsStringAsync();

        listContent.Should().Contain(uniqueTitle);
    }

    [Fact]
    public async Task FullFlow_AdminCreatesGame_UserSearchesByTitle_Finds()
    {
        var adminClient = _factory.CreateClientWithToken("Admin");
        var userClient = _factory.CreateClientWithToken("User");
        var uniqueTitle = $"Searchable Game {Guid.NewGuid()}";

        // Admin creates game
        await adminClient.PostAsync(BaseRoute, Json(new GameDto { Title = uniqueTitle, Price = 39.99m, Genre = "RPG" }));

        // User searches by title
        var searchResponse = await userClient.GetAsync($"{BaseRoute}/search?title={Uri.EscapeDataString(uniqueTitle)}&page=1&pageSize=10");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await searchResponse.Content.ReadAsStringAsync();

        content.Should().Contain(uniqueTitle);
    }

    [Fact]
    public async Task FullFlow_AdminCreatesGame_AdminUpdates_AdminDeletes()
    {
        var adminClient = _factory.CreateClientWithToken("Admin");

        // Create
        var createBody = Json(new GameDto { Title = "Lifecycle Game", Price = 49.99m, Genre = "FPS" });
        var createResponse = await adminClient.PostAsync(BaseRoute, createBody);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var gameId = ExtractIdFromBody(createContent);

        // Update
        var updateBody = Json(new GameDto { Title = "Lifecycle Game Updated", Price = 39.99m, IsActive = true });
        var updateResponse = await adminClient.PatchAsync($"{BaseRoute}/{gameId}", updateBody);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateContent = await updateResponse.Content.ReadAsStringAsync();
        updateContent.Should().Contain("Lifecycle Game Updated");

        // Delete
        var deleteResponse = await adminClient.DeleteAsync($"{BaseRoute}/{gameId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify deleted — expect 400 on delete again
        var deletedAgain = await adminClient.DeleteAsync($"{BaseRoute}/{gameId}");
        deletedAgain.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FullFlow_AcquireSameGameTwice_SecondReturns400()
    {
        var game = await _factory.SeedGameAsync("Duplicate Acquire Flow");
        // Seed existing acquisition for Guid.Empty (the userId the controller currently resolves to)
        await _factory.SeedAcquisitionAsync(Guid.Empty, game.Id, game.Price);

        var userClient = _factory.CreateClientWithToken("User");

        // First attempt: already owned (due to seed)
        var firstResponse = await userClient.PostAsync($"{BaseRoute}/{game.Id}/acquire", null);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FullFlow_UserCannotCreateOrDelete_OnlyAcquire()
    {
        var adminClient = _factory.CreateClientWithToken("Admin");
        var userClient = _factory.CreateClientWithToken("User");

        // Admin creates a game
        var game = await _factory.SeedGameAsync("Role Flow Game");

        // User tries to create → 403
        var createBody = Json(new GameDto { Title = "Unauthorized Create", Price = 1m });
        var createResponse = await userClient.PostAsync(BaseRoute, createBody);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // User tries to delete → 403
        var deleteResponse = await userClient.DeleteAsync($"{BaseRoute}/{game.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // User can acquire → 202
        var acquireResponse = await userClient.PostAsync($"{BaseRoute}/{game.Id}/acquire", null);
        acquireResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}
