using FCG_CATALOG_API.Domain.Entities;
using FCG_CATALOG_API.Infra;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Testcontainers.PostgreSql;

namespace FCG_CATALOG_API.Tests.Integration.Config;

public class CustomWebApplicationFactory : WebApplicationFactory<FCG_CATALOG_API.Api.Startup>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("fcg_catalog_test")
        .WithUsername("fcg_test")
        .WithPassword("fcg_test_pass")
        .Build();

    public const string TestJwtSecret = "test-super-secret-key-that-is-at-least-32-chars!!";
    public const string TestJwtIssuer = "FCG.UsersAPI";
    public const string TestJwtAudience = "FCG.Client";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                ["Jwt:SecretKey"] = TestJwtSecret,
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["RabbitMq:Host"] = "localhost",
                ["RabbitMq:Username"] = "guest",
                ["RabbitMq:Password"] = "guest"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove MassTransit hosted services (they try to connect to RabbitMQ on startup)
            var hostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                            (d.ImplementationType?.FullName?.StartsWith("MassTransit") == true ||
                             d.ImplementationFactory?.Method.DeclaringType?.FullName?.StartsWith("MassTransit") == true))
                .ToList();
            foreach (var d in hostedServices) services.Remove(d);

            // Replace IPublishEndpoint with a no-op mock so GameService.AcquireGame works in tests
            var publishDescriptors = services
                .Where(d => d.ServiceType == typeof(IPublishEndpoint))
                .ToList();
            foreach (var d in publishDescriptors) services.Remove(d);

            services.AddSingleton(Mock.Of<IPublishEndpoint>());
        });
    }

    public IServiceScope CreateTestScope() => Services.CreateScope();

    public async Task<Game> SeedGameAsync(string title = "Test Game", decimal price = 49.99m, string genre = "RPG")
    {
        using var scope = CreateTestScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var game = new Game(title, "Integration test game", price, genre);
        db.Games.Add(game);
        await db.SaveChangesAsync();
        return game;
    }

    public async Task SeedAcquisitionAsync(Guid userId, Guid gameId, decimal pricePaid = 49.99m)
    {
        using var scope = CreateTestScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Acquisitions.Add(new Acquisition
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GameId = gameId,
            PricePaid = pricePaid,
            AcquisitionDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public HttpClient CreateClientWithToken(string role, Guid? userId = null)
    {
        var client = CreateClient();
        var token = GenerateToken(userId ?? Guid.NewGuid(), role);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static string GenerateToken(Guid userId, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Email, $"test-{role.ToLower()}@test.com"),
            new Claim(ClaimTypes.Name, $"Test {role}")
        };

        var token = new JwtSecurityToken(
            issuer: TestJwtIssuer,
            audience: TestJwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public new async Task DisposeAsync() => await _postgres.DisposeAsync();
}
