using Bogus;
using FCG_CATALOG_API.Domain.Entities;
using FCG_CATALOG_API.Infra;
using FCG_CATALOG_API.Infra.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FCG_CATALOG_API.Tests.Unit.Infra.Repositories;

public class GameRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly GameRepository _sut;
    private readonly Faker _faker;

    public GameRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _sut = new GameRepository(_context);
        _faker = new Faker("pt_BR");
    }

    public void Dispose() => _context.Dispose();

    private Game CreateFakeGame() =>
        new(_faker.Commerce.ProductName(), _faker.Lorem.Sentence(),
            _faker.Finance.Amount(1, 299), _faker.Commerce.Categories(1)[0]);

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await _sut.GetAllAsync();

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithGames_ReturnsAllGames()
    {
        await _sut.AddAsync(CreateFakeGame());
        await _sut.AddAsync(CreateFakeGame());
        await _sut.AddAsync(CreateFakeGame());

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingGame_ReturnsGame()
    {
        var game = CreateFakeGame();
        await _sut.AddAsync(game);

        var result = await _sut.GetByIdAsync(game.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(game.Id);
        result.Title.Should().Be(game.Title);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ValidGame_PersistsAndReturnsGame()
    {
        var game = CreateFakeGame();

        var result = await _sut.AddAsync(game);

        result.Should().NotBeNull();
        result!.Id.Should().Be(game.Id);

        var persisted = await _context.Games.FindAsync(game.Id);
        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task AddAsync_ValidGame_SetsSlugAutomatically()
    {
        var game = new Game("Meu Jogo Incrivel", "desc", 49.99m, "RPG");

        var result = await _sut.AddAsync(game);

        result!.Slug.Should().NotBeNullOrWhiteSpace();
        result.Slug.Should().Be("meu-jogo-incrivel");
    }

    [Fact]
    public async Task AddAsync_ValidGame_IsActiveByDefault()
    {
        var result = await _sut.AddAsync(CreateFakeGame());

        result!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_MultipleGames_IncreasesCount()
    {
        await _sut.AddAsync(CreateFakeGame());
        await _sut.AddAsync(CreateFakeGame());

        var all = await _sut.GetAllAsync();
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_ExistingGame_PersistsTitleChange()
    {
        var game = CreateFakeGame();
        await _sut.AddAsync(game);

        game.Update("Título Atualizado", null, null, null, null);
        await _sut.UpdateAsync(game);

        var updated = await _context.Games.FindAsync(game.Id);
        updated!.Title.Should().Be("Título Atualizado");
    }

    [Fact]
    public async Task UpdateAsync_ExistingGame_PersistsPriceChange()
    {
        var game = CreateFakeGame();
        await _sut.AddAsync(game);

        game.Update(null, null, 9.99m, null, null);
        await _sut.UpdateAsync(game);

        var updated = await _context.Games.FindAsync(game.Id);
        updated!.Price.Should().Be(9.99m);
    }

    [Fact]
    public async Task UpdateAsync_ExistingGame_PersistsIsActiveChange()
    {
        var game = CreateFakeGame();
        await _sut.AddAsync(game);

        game.Update(null, null, null, null, false);
        await _sut.UpdateAsync(game);

        var updated = await _context.Games.FindAsync(game.Id);
        updated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ExistingGame_RemovesFromDatabase()
    {
        var game = CreateFakeGame();
        await _sut.AddAsync(game);

        await _sut.DeleteAsync(game);

        var deleted = await _context.Games.FindAsync(game.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ExistingGame_DecreasesCount()
    {
        var game1 = CreateFakeGame();
        var game2 = CreateFakeGame();
        await _sut.AddAsync(game1);
        await _sut.AddAsync(game2);

        await _sut.DeleteAsync(game1);

        var all = await _sut.GetAllAsync();
        all.Should().HaveCount(1);
        all.Should().NotContain(g => g.Id == game1.Id);
    }

    [Fact]
    public async Task GetByIdAsync_AfterDelete_ReturnsNull()
    {
        var game = CreateFakeGame();
        await _sut.AddAsync(game);
        await _sut.DeleteAsync(game);

        var result = await _sut.GetByIdAsync(game.Id);

        result.Should().BeNull();
    }
}
