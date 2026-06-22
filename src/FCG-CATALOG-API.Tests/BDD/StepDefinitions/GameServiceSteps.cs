using AutoBogus;
using FCG_CATALOG_API.Application.DTOs;
using FCG_CATALOG_API.Application.Events;
using FCG_CATALOG_API.Application.Services;
using FCG_CATALOG_API.Domain.Common;
using FCG_CATALOG_API.Domain.Entities;
using FCG_CATALOG_API.Domain.Interfaces;
using FluentAssertions;
using MassTransit;
using Moq;
using Reqnroll;

namespace FCG_CATALOG_API.Tests.BDD.StepDefinitions;

[Binding]
public class GameServiceSteps
{
    private readonly Mock<IAcquisitionRepository> _acquisitionRepositoryMock = new();
    private readonly Mock<IGameRepository> _gameRepositoryMock = new();
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();
    private GameService _sut = null!;

    private Guid _userId;
    private Guid _gameId;
    private Game? _resultGame;
    private string? _resultMessage;
    private Exception? _thrownException;
    private Result<string>? _acquireResult;
    private List<Game> _resultGames = new();
    private List<Game> _gamesInRepo = new();

    public GameServiceSteps()
    {
        _sut = new GameService(
            _acquisitionRepositoryMock.Object,
            _gameRepositoryMock.Object,
            _publishEndpointMock.Object);
    }

    // ---- AcquireGame ----

    [Given(@"o usuário já possui o jogo em sua biblioteca")]
    public void DadoOUsuarioJaPossuiOJogoEmSuaBiblioteca()
    {
        _userId = Guid.NewGuid();
        _gameId = Guid.NewGuid();
        var acquisition = new Acquisition
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            GameId = _gameId,
            PricePaid = 59.99m,
            AcquisitionDate = DateTime.UtcNow
        };
        _acquisitionRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Acquisition> { acquisition });
    }

    [Given(@"o jogo não existe no catálogo")]
    public void DadoOJogoNaoExisteNoCatalogo()
    {
        _userId = Guid.NewGuid();
        _gameId = Guid.NewGuid();
        _acquisitionRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Acquisition>());
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(_gameId))
            .ReturnsAsync((Game?)null);
    }

    [Given(@"um jogo disponível no catálogo")]
    public void DadoUmJogoDisponivelNoCatalogo()
    {
        _userId = Guid.NewGuid();
        var game = new Game("Jogo Teste", "Descrição", 59.99m, "RPG");
        _gameId = game.Id;

        _acquisitionRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Acquisition>());
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(_gameId))
            .ReturnsAsync(game);
        _gameRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Game>()))
            .Returns(Task.CompletedTask);
        _gameRepositoryMock.Setup(r => r.DeleteAsync(It.IsAny<Game>()))
            .Returns(Task.CompletedTask);
    }

    [When(@"o usuário tenta adquirir o mesmo jogo novamente")]
    public async Task QuandoOUsuarioTentaAdquirirOMesmoJogoNovamente()
    {
        _acquireResult = await _sut.AcquireGame(new AcquireGameDto { UserId = _userId, GameId = _gameId });
    }

    [When(@"o usuário tenta adquirir o jogo inexistente")]
    public async Task QuandoOUsuarioTentaAdquirirOJogoInexistente()
    {
        _acquireResult = await _sut.AcquireGame(new AcquireGameDto { UserId = _userId, GameId = _gameId });
    }

    [When(@"o usuário adquire o jogo com sucesso")]
    public async Task QuandoOUsuarioAdquireOJogoComSucesso()
    {
        _acquireResult = await _sut.AcquireGame(new AcquireGameDto { UserId = _userId, GameId = _gameId });
    }

    [Then(@"o resultado deve ser falha com mensagem ""(.*)""")]
    public void EntaoOResultadoDeveSerFalhaComMensagem(string mensagem)
    {
        _acquireResult.Should().NotBeNull();
        _acquireResult!.IsSuccess.Should().BeFalse();
        _acquireResult.Errors.Should().Contain(mensagem);
    }

    [Then(@"o resultado deve ser sucesso")]
    public void EntaoOResultadoDeveSerSucesso()
    {
        _acquireResult.Should().NotBeNull();
        _acquireResult!.IsSuccess.Should().BeTrue();
    }

    [Then(@"o evento de pedido deve ter sido publicado")]
    public void EntaoOEventoDePedidoDeveTerSidoPublicado()
    {
        _publishEndpointMock.Verify(
            p => p.Publish(It.IsAny<OrderPlacedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---- CreateGame ----

    [When(@"o sistema tenta criar um jogo com título ""(.*)"" e preço (.*)")]
    public async Task QuandoOSistemaTentaCriarUmJogoComTituloEPreco(string titulo, decimal preco)
    {
        try { _resultGame = await _sut.CreateGameAsync(new GameDto { Title = titulo, Price = preco }); }
        catch (Exception ex) { _thrownException = ex; }
    }

    [When(@"o sistema tenta criar um jogo sem título e com preço (.*)")]
    public async Task QuandoOSistemaTentaCriarUmJogoSemTituloEComPreco(decimal preco)
    {
        try { _resultGame = await _sut.CreateGameAsync(new GameDto { Title = null, Price = preco }); }
        catch (Exception ex) { _thrownException = ex; }
    }

    [When(@"o sistema tenta criar um jogo com título ""(.*)"" e sem preço")]
    public async Task QuandoOSistemaTentaCriarUmJogoComTituloESemPreco(string titulo)
    {
        try { _resultGame = await _sut.CreateGameAsync(new GameDto { Title = titulo, Price = null }); }
        catch (Exception ex) { _thrownException = ex; }
    }

    [When(@"o sistema cria um jogo com título ""(.*)"" descrição ""(.*)"" preço (.*) e gênero ""(.*)""")]
    public async Task QuandoOSistemaCriaUmJogo(string titulo, string descricao, decimal preco, string genero)
    {
        var game = new Game(titulo, descricao, preco, genero);
        _gameRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Game>())).ReturnsAsync(game);
        try { _resultGame = await _sut.CreateGameAsync(new GameDto { Title = titulo, Description = descricao, Price = preco, Genre = genero }); }
        catch (Exception ex) { _thrownException = ex; }
    }

    [Then(@"deve ser lançada uma exceção com mensagem ""(.*)""")]
    public void EntaoDeveSerLancadaUmaExcecaoComMensagem(string mensagem)
    {
        _thrownException.Should().NotBeNull();
        _thrownException!.Message.Should().Be(mensagem);
    }

    [Then(@"o jogo criado deve ter título ""(.*)""")]
    public void EntaoOJogoCriadoDeveTerTitulo(string titulo)
    {
        _resultGame.Should().NotBeNull();
        _resultGame!.Title.Should().Be(titulo);
    }

    [Then(@"o jogo criado deve ter preço (.*)")]
    public void EntaoOJogoCriadoDeveTerPreco(decimal preco)
    {
        _resultGame!.Price.Should().Be(preco);
    }

    [Then(@"o repositório de jogos deve ter sido chamado para adicionar")]
    public void EntaoORepositorioDeJogosDeveTerSidoChamadoParaAdicionar()
    {
        _gameRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Game>()), Times.Once);
    }

    [Then(@"o jogo criado deve estar ativo")]
    public void EntaoOJogoCriadoDeveEstarAtivo()
    {
        _resultGame!.IsActive.Should().BeTrue();
    }

    // ---- GetGames ----

    [Given(@"(\d+) jogos cadastrados no sistema")]
    public void DadoNJogosCadastradosNoSistema(int quantidade)
    {
        _gamesInRepo = Enumerable.Range(1, quantidade)
            .Select(i => new Game($"Jogo {i}", "Desc", 49.99m + i, "Action"))
            .ToList();
        _gameRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(_gamesInRepo);
    }

    [Given(@"os seguintes jogos cadastrados no sistema:")]
    public void DadoOsSeguintesJogosCadastradosNoSistema(DataTable dataTable)
    {
        _gamesInRepo = dataTable.Rows
            .Select(row => new Game(
                row["Titulo"],
                string.Empty,
                decimal.Parse(row["Preco"], System.Globalization.CultureInfo.InvariantCulture),
                row["Genero"]))
            .ToList();
        _gameRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(_gamesInRepo);
    }

    private static FiltersDto BuildFilters(string? title = null, string? genre = null, decimal? price = null, int? page = null, int? pageSize = null) =>
        new AutoFaker<FiltersDto>()
            .RuleFor(f => f.Title, _ => title)
            .RuleFor(f => f.Genre, _ => genre)
            .RuleFor(f => f.Price, _ => price)
            .RuleFor(f => f.Page, _ => page ?? 1)
            .RuleFor(f => f.PageSize, _ => pageSize ?? 20)
            .Generate();

    [When(@"todos os jogos são listados")]
    public async Task QuandoTodosOsJogosSaoListados()
    {
        _resultGames = (await _sut.GetGameAsync(BuildFilters())).ToList();
    }

    [When(@"os jogos são filtrados pelo título ""(.*)""")]
    public async Task QuandoOsJogosSaoFiltradosPeloTitulo(string titulo)
    {
        _resultGames = (await _sut.GetGameAsync(BuildFilters(title: titulo))).ToList();
    }

    [When(@"os jogos são filtrados pelo gênero ""(.*)""")]
    public async Task QuandoOsJogosSaoFiltradosPeloGenero(string genero)
    {
        _resultGames = (await _sut.GetGameAsync(BuildFilters(genre: genero))).ToList();
    }

    [When(@"os jogos são filtrados pelo preço máximo (.*)")]
    public async Task QuandoOsJogosSaoFiltradosPeloPrecoMaximo(decimal preco)
    {
        _resultGames = (await _sut.GetGameAsync(BuildFilters(price: preco))).ToList();
    }

    [When(@"os jogos são buscados na página (\d+) com tamanho de página (\d+)")]
    public async Task QuandoOsJogosSaoBuscadosNaPagina(int pagina, int tamanhoPagina)
    {
        _resultGames = (await _sut.GetGameAsync(BuildFilters(page: pagina, pageSize: tamanhoPagina))).ToList();
    }

    [Then(@"devem ser retornados (\d+) jogos")]
    public void EntaoDevemSerRetornadosNJogos(int quantidade)
    {
        _resultGames.Should().HaveCount(quantidade);
    }

    [Then(@"todos os jogos retornados devem conter ""(.*)"" no título")]
    public void EntaoTodosOsJogosRetornadosDevemConterNoTitulo(string texto)
    {
        _resultGames.Should().AllSatisfy(g => g.Title.Should().Contain(texto));
    }

    [Then(@"todos os jogos retornados devem ser do gênero ""(.*)""")]
    public void EntaoTodosOsJogosRetornadosDevemSerDoGenero(string genero)
    {
        _resultGames.Should().AllSatisfy(g => g.Genre.Should().Contain(genero));
    }

    [Then(@"todos os jogos retornados devem ter preço até (.*)")]
    public void EntaoTodosOsJogosRetornadosDevemTerPrecoAte(decimal preco)
    {
        _resultGames.Should().AllSatisfy(g => g.Price.Should().BeLessThanOrEqualTo(preco));
    }

    // ---- UpdateGame ----

    [When(@"o sistema tenta atualizar um jogo sem informar o id")]
    public async Task QuandoOSistemaTentaAtualizarUmJogoSemInformarOId()
    {
        try { await _sut.UpdateGameAsync(new GameDto { Id = null, Title = "Titulo" }); }
        catch (Exception ex) { _thrownException = ex; }
    }

    [When(@"o sistema tenta atualizar o jogo inexistente")]
    public async Task QuandoOSistemaTentaAtualizarOJogoInexistente()
    {
        try { await _sut.UpdateGameAsync(new GameDto { Id = _gameId, Title = "Titulo" }); }
        catch (Exception ex) { _thrownException = ex; }
    }

    [When(@"o sistema atualiza o jogo com título ""(.*)"" e preço (.*)")]
    public async Task QuandoOSistemaAtualizaOJogoComTituloEPreco(string titulo, decimal preco)
    {
        try { _resultGame = await _sut.UpdateGameAsync(new GameDto { Id = _gameId, Title = titulo, Price = preco }); }
        catch (Exception ex) { _thrownException = ex; }
    }

    [Then(@"o jogo atualizado deve ter título ""(.*)""")]
    public void EntaoOJogoAtualizadoDeveTerTitulo(string titulo)
    {
        _resultGame.Should().NotBeNull();
        _resultGame!.Title.Should().Be(titulo);
    }

    [Then(@"o jogo atualizado deve ter preço (.*)")]
    public void EntaoOJogoAtualizadoDeveTerPreco(decimal preco)
    {
        _resultGame!.Price.Should().Be(preco);
    }

    [Then(@"o repositório de jogos deve ter sido chamado para atualizar")]
    public void EntaoORepositorioDeJogosDeveTerSidoChamadoParaAtualizar()
    {
        _gameRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Game>()), Times.Once);
    }

    // ---- DeleteGame ----

    [When(@"o sistema tenta remover o jogo inexistente")]
    public async Task QuandoOSistemaTentaRemoverOJogoInexistente()
    {
        try { await _sut.DeleteGameAsync(_gameId); }
        catch (Exception ex) { _thrownException = ex; }
    }

    [When(@"o sistema remove o jogo")]
    public async Task QuandoOSistemaRemoveOJogo()
    {
        try { _resultMessage = await _sut.DeleteGameAsync(_gameId); }
        catch (Exception ex) { _thrownException = ex; }
    }

    [Then(@"a mensagem retornada deve conter o título do jogo")]
    public void EntaoAMensagemRetornadaDeveConterOTituloDoJogo()
    {
        _resultMessage.Should().NotBeNull();
        _resultMessage.Should().Contain("Jogo Teste");
    }

    [Then(@"a mensagem retornada deve conter ""(.*)""")]
    public void EntaoAMensagemRetornadaDeveConter(string texto)
    {
        _resultMessage.Should().Contain(texto);
    }

    [Then(@"o repositório de jogos deve ter sido chamado para remover")]
    public void EntaoORepositorioDeJogosDeveTerSidoChamadoParaRemover()
    {
        _gameRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Game>()), Times.Once);
    }
}
