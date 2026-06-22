Feature: Gerenciamento de Jogos
  Como administrador do sistema
  Quero gerenciar o catálogo de jogos
  Para que usuários possam adquirir e consultar jogos

  # ---- AcquireGame ----

  Scenario: Adquirir jogo que o usuário já possui retorna falha
    Given o usuário já possui o jogo em sua biblioteca
    When o usuário tenta adquirir o mesmo jogo novamente
    Then o resultado deve ser falha com mensagem "Usuário já possui esse jogo"

  Scenario: Adquirir jogo inexistente retorna falha
    Given o jogo não existe no catálogo
    When o usuário tenta adquirir o jogo inexistente
    Then o resultado deve ser falha com mensagem "Jogo não encontrado"

  Scenario: Adquirir jogo disponível publica evento de pedido com sucesso
    Given um jogo disponível no catálogo
    When o usuário adquire o jogo com sucesso
    Then o resultado deve ser sucesso
    And o evento de pedido deve ter sido publicado

  # ---- CreateGame ----

  Scenario Outline: Criar jogo com título inválido lança exceção
    When o sistema tenta criar um jogo com título "<titulo>" e preço 49.99
    Then deve ser lançada uma exceção com mensagem "Título é obrigatório"

    Examples:
      | titulo |
      |        |

  Scenario: Criar jogo com título nulo lança exceção
    When o sistema tenta criar um jogo sem título e com preço 49.99
    Then deve ser lançada uma exceção com mensagem "Título é obrigatório"

  Scenario: Criar jogo sem preço lança exceção
    When o sistema tenta criar um jogo com título "Algum Jogo" e sem preço
    Then deve ser lançada uma exceção com mensagem "Preço é obrigatório"

  Scenario: Criar jogo com dados válidos retorna o jogo criado
    When o sistema cria um jogo com título "Cyberpunk 2077" descrição "RPG futurista" preço 199.90 e gênero "RPG"
    Then o jogo criado deve ter título "Cyberpunk 2077"
    And o jogo criado deve ter preço 199.90
    And o repositório de jogos deve ter sido chamado para adicionar

  Scenario: Jogo recém criado deve estar ativo por padrão
    When o sistema cria um jogo com título "Novo Jogo" descrição "" preço 59.99 e gênero "Action"
    Then o jogo criado deve estar ativo

  # ---- GetGames ----

  Scenario: Listar todos os jogos retorna a lista completa
    Given 3 jogos cadastrados no sistema
    When todos os jogos são listados
    Then devem ser retornados 3 jogos

  Scenario: Filtrar jogos por título retorna apenas os correspondentes
    Given os seguintes jogos cadastrados no sistema:
      | Titulo         | Preco | Genero |
      | Call of Duty   | 59.99 | FPS    |
      | FIFA 24        | 49.99 | Sports |
      | Call of Duty 2 | 59.99 | FPS    |
    When os jogos são filtrados pelo título "Call of Duty"
    Then devem ser retornados 2 jogos
    And todos os jogos retornados devem conter "Call of Duty" no título

  Scenario: Filtrar jogos por gênero retorna apenas os correspondentes
    Given os seguintes jogos cadastrados no sistema:
      | Titulo | Preco | Genero |
      | Game A | 49.99 | RPG    |
      | Game B | 59.99 | FPS    |
      | Game C | 39.99 | RPG    |
    When os jogos são filtrados pelo gênero "RPG"
    Then devem ser retornados 2 jogos
    And todos os jogos retornados devem ser do gênero "RPG"

  Scenario: Filtrar jogos por preço máximo retorna apenas os que cabem no orçamento
    Given os seguintes jogos cadastrados no sistema:
      | Titulo | Preco | Genero |
      | Game A | 20.00 | RPG    |
      | Game B | 60.00 | FPS    |
      | Game C | 50.00 | RPG    |
    When os jogos são filtrados pelo preço máximo 50.00
    Then devem ser retornados 2 jogos
    And todos os jogos retornados devem ter preço até 50.00

  Scenario: Paginação retorna apenas os jogos da página solicitada
    Given 10 jogos cadastrados no sistema
    When os jogos são buscados na página 2 com tamanho de página 3
    Then devem ser retornados 3 jogos

  # ---- UpdateGame ----

  Scenario: Atualizar jogo sem informar o id lança exceção
    When o sistema tenta atualizar um jogo sem informar o id
    Then deve ser lançada uma exceção com mensagem "Id do jogo é obrigatório para atualização"

  Scenario: Atualizar jogo inexistente lança exceção
    Given o jogo não existe no catálogo
    When o sistema tenta atualizar o jogo inexistente
    Then deve ser lançada uma exceção com mensagem "Jogo não encontrado"

  Scenario: Atualizar jogo com dados válidos retorna jogo atualizado
    Given um jogo disponível no catálogo
    When o sistema atualiza o jogo com título "Novo Título" e preço 39.99
    Then o jogo atualizado deve ter título "Novo Título"
    And o jogo atualizado deve ter preço 39.99
    And o repositório de jogos deve ter sido chamado para atualizar

  # ---- DeleteGame ----

  Scenario: Remover jogo inexistente lança exceção
    Given o jogo não existe no catálogo
    When o sistema tenta remover o jogo inexistente
    Then deve ser lançada uma exceção com mensagem "Jogo não encontrado"

  Scenario: Remover jogo existente retorna mensagem de sucesso
    Given um jogo disponível no catálogo
    When o sistema remove o jogo
    Then a mensagem retornada deve conter o título do jogo
    And a mensagem retornada deve conter "removido com sucesso"
    And o repositório de jogos deve ter sido chamado para remover
