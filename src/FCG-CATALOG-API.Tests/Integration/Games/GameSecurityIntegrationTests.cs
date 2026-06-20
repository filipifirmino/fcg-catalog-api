using FCG_CATALOG_API.Tests.Integration.Config;
using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace FCG_CATALOG_API.Tests.Integration.Games;

public class GameSecurityIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private const string BaseRoute = "/api/v1/games";

    public GameSecurityIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static StringContent Json(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    // ---- Unauthenticated access → 401 ----

    [Theory]
    [InlineData("GET", "/api/v1/games")]
    [InlineData("GET", "/api/v1/games/search")]
    public async Task Get_Endpoints_Unauthenticated_Return401(string method, string route)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), route);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostCreate_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var body = Json(new { Title = "Hacking", Price = 1.00m });

        var response = await client.PostAsync(BaseRoute, body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Patch_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsync($"{BaseRoute}/{Guid.NewGuid()}", Json(new { Title = "x", Price = 1m }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"{BaseRoute}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Acquire_Unauthenticated_Returns401()
    {
        var game = await _factory.SeedGameAsync("Security Game");
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"{BaseRoute}/{game.Id}/acquire", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- User accessing Admin-only endpoints → 403 ----

    [Fact]
    public async Task CreateGame_AsUser_Returns403()
    {
        var client = _factory.CreateClientWithToken("User");
        var body = Json(new { Title = "Jogo Negado", Price = 49.99m });

        var response = await client.PostAsync(BaseRoute, body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateGame_AsUser_Returns403()
    {
        var game = await _factory.SeedGameAsync("Security Update Test");
        var client = _factory.CreateClientWithToken("User");

        var response = await client.PatchAsync($"{BaseRoute}/{game.Id}", Json(new { Title = "Tentativa", Price = 1m }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteGame_AsUser_Returns403()
    {
        var game = await _factory.SeedGameAsync("Security Delete Test");
        var client = _factory.CreateClientWithToken("User");

        var response = await client.DeleteAsync($"{BaseRoute}/{game.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- Expired/invalid token → 401 ----

    [Fact]
    public async Task GetAllGames_WithExpiredToken_Returns401()
    {
        var client = _factory.CreateClient();
        var expiredToken = GenerateExpiredToken();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await client.GetAsync(BaseRoute);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllGames_WithInvalidToken_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "this.is.not.a.valid.token");

        var response = await client.GetAsync(BaseRoute);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- Admin can access all admin endpoints ----

    [Fact]
    public async Task GetAllGames_AsAdmin_Returns200()
    {
        var client = _factory.CreateClientWithToken("Admin");

        var response = await client.GetAsync(BaseRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateGame_AsAdmin_Returns201()
    {
        var client = _factory.CreateClientWithToken("Admin");
        var body = Json(new { Title = "Security Admin Game", Price = 79.99m, Genre = "Action" });

        var response = await client.PostAsync(BaseRoute, body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static string GenerateExpiredToken()
    {
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(CustomWebApplicationFactory.TestJwtSecret));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "User")
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: CustomWebApplicationFactory.TestJwtIssuer,
            audience: CustomWebApplicationFactory.TestJwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(-1),
            signingCredentials: creds);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
