using FCG_CATALOG_API.Application.DTOs;
using FCG_CATALOG_API.Application.Events;
using FCG_CATALOG_API.Application.Interfaces;
using FCG_CATALOG_API.Domain.Common;
using FCG_CATALOG_API.Domain.Entities;
using FCG_CATALOG_API.Domain.Interfaces;
using MassTransit;

namespace FCG_CATALOG_API.Application.Services;

public class GameService : IGameService
{
    private readonly IAcquisitionRepository _acquisitionRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public GameService(
        IAcquisitionRepository acquisitionRepository,
        IGameRepository gameRepository,
        IPublishEndpoint publishEndpoint)
    {
        _acquisitionRepository = acquisitionRepository;
        _gameRepository = gameRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<string>> AcquireGame(AcquireGameDto dto)
    {
        var all = await _acquisitionRepository.GetAllAsync();
        if (all.Any(a => a.UserId == dto.UserId && a.GameId == dto.GameId))
            return Result<string>.Failure("Usuário já possui esse jogo");

        var game = await _gameRepository.GetByIdAsync(dto.GameId);
        if (game is null)
            return Result<string>.Failure("Jogo não encontrado");

        await _publishEndpoint.Publish(new OrderPlacedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = dto.UserId,
            GameId = game.Id,
            GameTitle = game.Title,
            Price = game.Price,
            PlacedAt = DateTime.UtcNow
        });

        return Result<string>.Success("Pedido recebido, aguardando processamento do pagamento.");
    }

    public async Task<Game> CreateGameAsync(GameDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new Exception("Título é obrigatório");
        if (!dto.Price.HasValue)
            throw new Exception("Preço é obrigatório");

        var game = new Game(dto.Title, dto.Description ?? string.Empty, dto.Price.Value, dto.Genre ?? string.Empty);
        var created = await _gameRepository.AddAsync(game);
        return created!;
    }

    public async Task<IEnumerable<Game>> GetGamesAsync()
        => await _gameRepository.GetAllAsync();

    public async Task<IEnumerable<Game>> GetGameAsync(FiltersDto filters)
    {
        var games = await _gameRepository.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(filters.Title))
            games = games.Where(g => g.Title.Contains(filters.Title, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(filters.Genre))
            games = games.Where(g => g.Genre.Contains(filters.Genre, StringComparison.OrdinalIgnoreCase));

        if (filters.Price.HasValue)
            games = games.Where(g => g.Price <= filters.Price.Value);

        int page = filters.Page ?? 1;
        int pageSize = filters.PageSize ?? 20;
        games = games.Skip((page - 1) * pageSize).Take(pageSize);

        return games;
    }

    public async Task<Game> UpdateGameAsync(GameDto dto)
    {
        if (!dto.Id.HasValue)
            throw new Exception("Id do jogo é obrigatório para atualização");

        var game = await _gameRepository.GetByIdAsync(dto.Id.Value)
            ?? throw new Exception("Jogo não encontrado");

        game.Update(dto.Title, dto.Description, dto.Price, dto.Genre, dto.IsActive);
        await _gameRepository.UpdateAsync(game);
        return game;
    }

    public async Task<string> DeleteGameAsync(Guid gameId)
    {
        var game = await _gameRepository.GetByIdAsync(gameId)
            ?? throw new Exception("Jogo não encontrado");

        await _gameRepository.DeleteAsync(game);
        return $"Jogo '{game.Title}' removido com sucesso";
    }
}
