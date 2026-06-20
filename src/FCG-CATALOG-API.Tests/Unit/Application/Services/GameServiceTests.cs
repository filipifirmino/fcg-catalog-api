using AutoBogus;
using Bogus;
using FCG_CATALOG_API.Application.DTOs;
using FCG_CATALOG_API.Application.Events;
using FCG_CATALOG_API.Application.Services;
using FCG_CATALOG_API.Domain.Entities;
using FCG_CATALOG_API.Domain.Interfaces;
using FluentAssertions;
using MassTransit;
using Moq;

namespace FCG_CATALOG_API.Tests.Unit.Application.Services;

public class GameServiceTests
{
    private readonly Mock<IAcquisitionRepository> _acquisitionRepositoryMock;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly GameService _sut;
    private readonly Faker _faker;

    public GameServiceTests()
    {
        _acquisitionRepositoryMock = new Mock<IAcquisitionRepository>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _sut = new GameService(
            _acquisitionRepositoryMock.Object,
            _gameRepositoryMock.Object,
            _publishEndpointMock.Object);
        _faker = new Faker("pt_BR");
    }

    private Game CreateFakeGame() =>
        new(_faker.Commerce.ProductName(), _faker.Lorem.Sentence(),
            _faker.Finance.Amount(1, 299), _faker.Commerce.Categories(1)[0]);

    // ---- AcquireGame ----

    [Fact]
    public async Task AcquireGame_UserAlreadyOwnsGame_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var acquisition = new Acquisition { Id = Guid.NewGuid(), UserId = userId, GameId = gameId };

        _acquisitionRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Acquisition> { acquisition });

        var result = await _sut.AcquireGame(new AcquireGameDto { UserId = userId, GameId = gameId });

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Usuário já possui esse jogo");
    }

    [Fact]
    public async Task AcquireGame_GameNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        _acquisitionRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Acquisition>());
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId))
            .ReturnsAsync((Game?)null);

        var result = await _sut.AcquireGame(new AcquireGameDto { UserId = userId, GameId = gameId });

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Jogo não encontrado");
    }

    [Fact]
    public async Task AcquireGame_ValidInput_PublishesOrderPlacedEvent()
    {
        var userId = Guid.NewGuid();
        var game = CreateFakeGame();

        _acquisitionRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Acquisition>());
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(game.Id))
            .ReturnsAsync(game);

        var result = await _sut.AcquireGame(new AcquireGameDto { UserId = userId, GameId = game.Id });

        result.IsSuccess.Should().BeTrue();
        _publishEndpointMock.Verify(
            p => p.Publish(It.IsAny<OrderPlacedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AcquireGame_ValidInput_PublishesEventWithCorrectGameData()
    {
        var userId = Guid.NewGuid();
        var game = CreateFakeGame();

        _acquisitionRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Acquisition>());
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(game.Id))
            .ReturnsAsync(game);

        await _sut.AcquireGame(new AcquireGameDto { UserId = userId, GameId = game.Id });

        _publishEndpointMock.Verify(
            p => p.Publish(
                It.Is<OrderPlacedEvent>(e =>
                    e.UserId == userId &&
                    e.GameId == game.Id &&
                    e.Price == game.Price &&
                    e.GameTitle == game.Title),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---- CreateGame ----

    [Fact]
    public async Task CreateGameAsync_EmptyTitle_ThrowsException()
    {
        var dto = new GameDto { Title = "", Price = 49.99m };

        var act = async () => await _sut.CreateGameAsync(dto);

        await act.Should().ThrowAsync<Exception>().WithMessage("Título é obrigatório");
    }

    [Fact]
    public async Task CreateGameAsync_NullTitle_ThrowsException()
    {
        var dto = new GameDto { Title = null, Price = 49.99m };

        var act = async () => await _sut.CreateGameAsync(dto);

        await act.Should().ThrowAsync<Exception>().WithMessage("Título é obrigatório");
    }

    [Fact]
    public async Task CreateGameAsync_NullPrice_ThrowsException()
    {
        var dto = new GameDto { Title = _faker.Commerce.ProductName(), Price = null };

        var act = async () => await _sut.CreateGameAsync(dto);

        await act.Should().ThrowAsync<Exception>().WithMessage("Preço é obrigatório");
    }

    [Fact]
    public async Task CreateGameAsync_ValidDto_CreatesAndReturnsGame()
    {
        var dto = new GameDto
        {
            Title = _faker.Commerce.ProductName(),
            Description = _faker.Lorem.Sentence(),
            Price = _faker.Finance.Amount(1, 299),
            Genre = _faker.Commerce.Categories(1)[0]
        };

        _gameRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Game>()))
            .ReturnsAsync((Game g) => g);

        var result = await _sut.CreateGameAsync(dto);

        result.Should().NotBeNull();
        result.Title.Should().Be(dto.Title);
        result.Price.Should().Be(dto.Price!.Value);
        _gameRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Game>()), Times.Once);
    }

    [Fact]
    public async Task CreateGameAsync_ValidDto_GameIsActiveByDefault()
    {
        var dto = new GameDto { Title = _faker.Commerce.ProductName(), Price = _faker.Finance.Amount(1, 299) };
        _gameRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Game>())).ReturnsAsync((Game g) => g);

        var result = await _sut.CreateGameAsync(dto);

        result.IsActive.Should().BeTrue();
    }

    // ---- GetGames ----

    [Fact]
    public async Task GetGamesAsync_WhenCalled_ReturnsAllGames()
    {
        var games = new List<Game> { CreateFakeGame(), CreateFakeGame(), CreateFakeGame() };
        _gameRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(games);

        var result = await _sut.GetGamesAsync();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetGameAsync_FilterByTitle_ReturnsMatchingGames()
    {
        var games = new List<Game>
        {
            new("Call of Duty", "desc", 59.99m, "FPS"),
            new("FIFA 24", "desc", 49.99m, "Sports"),
            new("Call of Duty 2", "desc", 59.99m, "FPS")
        };
        _gameRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(games);

        var filters = new AutoFaker<FiltersDto>()
            .RuleFor(f => f.Title, _ => "Call of Duty")
            .RuleFor(f => f.Genre, _ => (string?)null)
            .RuleFor(f => f.Price, _ => (decimal?)null)
            .RuleFor(f => f.Page, _ => 1)
            .RuleFor(f => f.PageSize, _ => 20)
            .Generate();

        var result = await _sut.GetGameAsync(filters);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(g => g.Title.Contains("Call of Duty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetGameAsync_FilterByGenre_ReturnsMatchingGames()
    {
        var games = new List<Game>
        {
            new("Game A", "desc", 49.99m, "RPG"),
            new("Game B", "desc", 59.99m, "FPS"),
            new("Game C", "desc", 39.99m, "RPG")
        };
        _gameRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(games);

        var filters = new AutoFaker<FiltersDto>()
            .RuleFor(f => f.Title, _ => (string?)null)
            .RuleFor(f => f.Genre, _ => "RPG")
            .RuleFor(f => f.Price, _ => (decimal?)null)
            .RuleFor(f => f.Page, _ => 1)
            .RuleFor(f => f.PageSize, _ => 20)
            .Generate();

        var result = await _sut.GetGameAsync(filters);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(g => g.Genre == "RPG");
    }

    [Fact]
    public async Task GetGameAsync_FilterByMaxPrice_ReturnsGamesWithinPrice()
    {
        var games = new List<Game>
        {
            new("Game A", "desc", 20.00m, "RPG"),
            new("Game B", "desc", 60.00m, "FPS"),
            new("Game C", "desc", 50.00m, "RPG")
        };
        _gameRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(games);

        var filters = new AutoFaker<FiltersDto>()
            .RuleFor(f => f.Title, _ => (string?)null)
            .RuleFor(f => f.Genre, _ => (string?)null)
            .RuleFor(f => f.Price, _ => 50.00m)
            .RuleFor(f => f.Page, _ => 1)
            .RuleFor(f => f.PageSize, _ => 20)
            .Generate();

        var result = await _sut.GetGameAsync(filters);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(g => g.Price <= 50.00m);
    }

    [Fact]
    public async Task GetGameAsync_WithPagination_ReturnsCorrectPage()
    {
        var games = Enumerable.Range(1, 10).Select(i => new Game($"Game {i}", "desc", 10m, "RPG")).ToList();
        _gameRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(games);

        var filters = new AutoFaker<FiltersDto>()
            .RuleFor(f => f.Title, _ => (string?)null)
            .RuleFor(f => f.Genre, _ => (string?)null)
            .RuleFor(f => f.Price, _ => (decimal?)null)
            .RuleFor(f => f.Page, _ => 2)
            .RuleFor(f => f.PageSize, _ => 3)
            .Generate();

        var result = await _sut.GetGameAsync(filters);

        result.Should().HaveCount(3);
    }

    // ---- UpdateGame ----

    [Fact]
    public async Task UpdateGameAsync_NoId_ThrowsException()
    {
        var dto = new AutoFaker<GameDto>().RuleFor(g => g.Id, _ => (Guid?)null).Generate();

        var act = async () => await _sut.UpdateGameAsync(dto);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Id do jogo é obrigatório para atualização");
    }

    [Fact]
    public async Task UpdateGameAsync_GameNotFound_ThrowsException()
    {
        var dto = new AutoFaker<GameDto>().RuleFor(g => g.Id, f => (Guid?)f.Random.Guid()).Generate();
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(dto.Id!.Value)).ReturnsAsync((Game?)null);

        var act = async () => await _sut.UpdateGameAsync(dto);

        await act.Should().ThrowAsync<Exception>().WithMessage("Jogo não encontrado");
    }

    [Fact]
    public async Task UpdateGameAsync_ValidDto_UpdatesAndReturnsGame()
    {
        var game = CreateFakeGame();
        var newTitle = _faker.Commerce.ProductName();
        var dto = new GameDto { Id = game.Id, Title = newTitle, Price = 39.99m, IsActive = true };

        _gameRepositoryMock.Setup(r => r.GetByIdAsync(game.Id)).ReturnsAsync(game);
        _gameRepositoryMock.Setup(r => r.UpdateAsync(game)).Returns(Task.CompletedTask);

        var result = await _sut.UpdateGameAsync(dto);

        result.Should().NotBeNull();
        result.Title.Should().Be(newTitle);
        result.Price.Should().Be(39.99m);
        _gameRepositoryMock.Verify(r => r.UpdateAsync(game), Times.Once);
    }

    // ---- DeleteGame ----

    [Fact]
    public async Task DeleteGameAsync_GameNotFound_ThrowsException()
    {
        var gameId = Guid.NewGuid();
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(gameId)).ReturnsAsync((Game?)null);

        var act = async () => await _sut.DeleteGameAsync(gameId);

        await act.Should().ThrowAsync<Exception>().WithMessage("Jogo não encontrado");
    }

    [Fact]
    public async Task DeleteGameAsync_ExistingGame_DeletesAndReturnsMessage()
    {
        var game = CreateFakeGame();
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(game.Id)).ReturnsAsync(game);
        _gameRepositoryMock.Setup(r => r.DeleteAsync(game)).Returns(Task.CompletedTask);

        var result = await _sut.DeleteGameAsync(game.Id);

        result.Should().Contain(game.Title);
        result.Should().Contain("removido com sucesso");
        _gameRepositoryMock.Verify(r => r.DeleteAsync(game), Times.Once);
    }
}
