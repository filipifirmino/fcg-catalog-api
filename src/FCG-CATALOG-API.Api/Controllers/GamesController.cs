using FCG_CATALOG_API.Api.Authorization;
using FCG_CATALOG_API.Api.Common;
using FCG_CATALOG_API.Application.DTOs;
using FCG_CATALOG_API.Application.Interfaces;
using FCG_CATALOG_API.Domain.Common;
using FCG_CATALOG_API.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FCG_CATALOG_API.Api.Controllers;

/// <summary>Gerenciamento de jogos da plataforma FCG.</summary>
[ApiController]
[Route("api/v1/games")]
[Produces("application/json")]
public class GamesController : BaseController
{
    private readonly IGameService _gameService;

    public GamesController(IGameService gameService)
    {
        _gameService = gameService;
    }

    /// <summary>Retorna todos os jogos cadastrados.</summary>
    /// <response code="200">Lista de jogos retornada com sucesso.</response>
    /// <response code="401">Token JWT ausente ou inválido.</response>
    /// <response code="403">Usuário não possui permissão para acessar este recurso.</response>
    [HttpGet]
    [Authorize(Policy = Policies.UserOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<Game>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllGamesAsync()
    {
        var games = await _gameService.GetGamesAsync();
        return CustomResponse(Result<IEnumerable<Game>>.Success(games));
    }

    /// <summary>Retorna jogos aplicando filtros de busca com paginação.</summary>
    /// <param name="filters">Filtros disponíveis: título, gênero, preço máximo, data de cadastro, página e tamanho da página.</param>
    /// <response code="200">Lista filtrada de jogos retornada com sucesso.</response>
    /// <response code="401">Token JWT ausente ou inválido.</response>
    /// <response code="403">Usuário não possui permissão para acessar este recurso.</response>
    [HttpGet("search")]
    [Authorize(Policy = Policies.UserOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<Game>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetGameAsync([FromQuery] FiltersDto filters)
    {
        var games = await _gameService.GetGameAsync(filters);
        return CustomResponse(Result<IEnumerable<Game>>.Success(games));
    }

    /// <summary>Cria um novo jogo. Acesso restrito a administradores.</summary>
    /// <param name="dto">Dados do jogo a ser criado. Título e preço são obrigatórios.</param>
    /// <response code="201">Jogo criado com sucesso.</response>
    /// <response code="400">Dados inválidos (ex: título ausente, preço negativo).</response>
    /// <response code="401">Token JWT ausente ou inválido.</response>
    /// <response code="403">Usuário não é administrador.</response>
    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<Game>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<Game>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateGameAsync([FromBody] GameDto dto)
    {
        try
        {
            var game = await _gameService.CreateGameAsync(dto);
            return CustomResponse(Result<Game>.Success(game), StatusCodes.Status201Created);
        }
        catch (Exception ex)
        {
            return CustomResponse(Result<Game>.Failure(ex.Message));
        }
    }

    /// <summary>Atualiza parcialmente os dados de um jogo. Acesso restrito a administradores.</summary>
    /// <param name="id">Identificador único do jogo a ser atualizado.</param>
    /// <param name="game">Campos a atualizar.</param>
    /// <response code="200">Jogo atualizado com sucesso.</response>
    /// <response code="400">Id não informado ou jogo não encontrado.</response>
    /// <response code="401">Token JWT ausente ou inválido.</response>
    /// <response code="403">Usuário não é administrador.</response>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<Game>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Game>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateGameAsync(Guid id, [FromBody] GameDto game)
    {
        try
        {
            game.Id = id;
            var updated = await _gameService.UpdateGameAsync(game);
            return CustomResponse(Result<Game>.Success(updated));
        }
        catch (Exception ex)
        {
            return CustomResponse(Result<Game>.Failure(ex.Message));
        }
    }

    /// <summary>Remove um jogo pelo seu identificador. Acesso restrito a administradores.</summary>
    /// <param name="id">Identificador único do jogo a ser removido.</param>
    /// <response code="200">Jogo removido com sucesso.</response>
    /// <response code="400">Jogo não encontrado.</response>
    /// <response code="401">Token JWT ausente ou inválido.</response>
    /// <response code="403">Usuário não é administrador.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteGameAsync(Guid id)
    {
        try
        {
            var message = await _gameService.DeleteGameAsync(id);
            return CustomResponse(Result<string>.Success(message));
        }
        catch (Exception ex)
        {
            return CustomResponse(Result<string>.Failure(ex.Message));
        }
    }

    /// <summary>Registra a aquisição de um jogo pelo usuário autenticado.</summary>
    /// <param name="id">Identificador do jogo a ser adquirido.</param>
    /// <response code="202">Solicitação de aquisição aceita.</response>
    /// <response code="400">Usuário já possui o jogo ou jogo não encontrado.</response>
    /// <response code="401">Token JWT ausente ou inválido.</response>
    /// <response code="403">Usuário não possui permissão para acessar este recurso.</response>
    [HttpPost("{id:guid}/acquire")]
    [Authorize(Policy = Policies.UserOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AcquireGameAsync(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
            var dto = new AcquireGameDto { GameId = id, UserId = userId };

            var result = await _gameService.AcquireGame(dto);
            return CustomResponse(result, StatusCodes.Status202Accepted);
        }
        catch (Exception ex)
        {
            return CustomResponse(Result<string>.Failure(ex.Message));
        }
    }
}
