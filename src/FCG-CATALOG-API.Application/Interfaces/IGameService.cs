using FCG_CATALOG_API.Application.DTOs;
using FCG_CATALOG_API.Domain.Common;
using FCG_CATALOG_API.Domain.Entities;

namespace FCG_CATALOG_API.Application.Interfaces;

public interface IGameService
{
    Task<Result<string>> AcquireGame(AcquireGameDto dto);
    Task<Game> CreateGameAsync(GameDto dto);
    Task<IEnumerable<Game>> GetGamesAsync();
    Task<IEnumerable<Game>> GetGameAsync(FiltersDto filters);
    Task<Game> UpdateGameAsync(GameDto games);
    Task<string> DeleteGameAsync(Guid gameId);
}
