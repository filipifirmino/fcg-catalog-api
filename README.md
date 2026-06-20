# FCG Catalog API — Grupo 14

Microsserviço responsável pelo catálogo de jogos e orquestração do fluxo de compra da plataforma **FIAP Cloud Games (FCG)**. Migrado do monolito `FCG_GRUPO_14` como parte da Fase 2 — migração para arquitetura de microsserviços.

---

## Sumário

- [Responsabilidade](#responsabilidade)
- [Arquitetura](#arquitetura)
- [Tecnologias](#tecnologias)
- [Domínio](#domínio)
- [Endpoints](#endpoints)
- [Eventos Publicados e Consumidos](#eventos-publicados-e-consumidos)
- [Pré-requisitos](#pré-requisitos)
- [Variáveis de Ambiente](#variáveis-de-ambiente)
- [Rodando com Docker](#rodando-com-docker)
- [Rodando localmente](#rodando-localmente)
- [Migrations](#migrations)
- [Testes](#testes)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Logs](#logs)

---

## Responsabilidade

| Item | Detalhe |
|---|---|
| Domínio | Catalog (Games + Acquisitions) |
| Tipo | Web API (HTTP) |
| Porta | 8081 (Docker) / 5000 (local) |
| Banco de dados | PostgreSQL — `fcg_catalog_db` |
| Publica evento | `OrderPlacedEvent` → RabbitMQ |
| Consome evento | `PaymentProcessedEvent` ← RabbitMQ |

---

## Arquitetura

O serviço adota **Clean Architecture**, com dependências fluindo sempre de fora para dentro (a camada de domínio não depende de nenhuma outra).

```
fcg-catalog-api/
└── src/
    ├── FCG-CATALOG-API.Domain        # Entidades, interfaces, Result<T>
    ├── FCG-CATALOG-API.Application   # Casos de uso: GameService, AcquisitionService, DTOs, Eventos
    ├── FCG-CATALOG-API.Infra         # EF Core, repositórios, consumers, migrations
    ├── FCG-CATALOG-API.Api           # Controllers, JWT, middlewares, Swagger
    └── FCG-CATALOG-API.Tests         # Testes unitários, de integração e BDD
```

**Fluxo de dependências:**
```
Api  →  Application  →  Domain
Infra  →  Application  →  Domain
Tests  →  Api | Application | Infra | Domain
```

### Padrões aplicados

- **Repository Pattern** — abstração de acesso a dados via `IRepositoryBase<T>` e interfaces específicas no domínio
- **Result Pattern** — `Result<T>` retornado pelos serviços em vez de exceções para falhas esperadas
- **DTOs** — separam o contrato HTTP das entidades de domínio
- **Middleware** — tratamento centralizado de exceções e tempo de resposta
- **Event-Driven Purchase Flow** — `POST /acquire` não persiste diretamente; publica `OrderPlacedEvent` e aguarda `PaymentProcessedEvent` para confirmar a aquisição

### Fluxo de compra

```
Monolito (antes):
  POST /acquire-game → GameService → salva Acquisition → retorna 200 OK

Microsserviço (agora):
  POST /api/v1/games/{id}/acquire
    └── Publica OrderPlacedEvent → retorna 202 Accepted

  PaymentProcessedConsumer (RabbitMQ):
    ├── Status = Approved → AcquisitionService.AddToLibraryAsync() → persiste Acquisition
    └── Status = Rejected → loga rejeição
```

---

## Tecnologias

| Camada | Tecnologia | Versão |
|---|---|---|
| Runtime | .NET | 10.0 |
| Framework Web | ASP.NET Core Web API | 10.0 |
| ORM | Entity Framework Core + Npgsql | 10.0.9 |
| Convenção de nomes | EFCore.NamingConventions (snake_case) | 10.0.1 |
| Banco de dados | PostgreSQL | 16 |
| Autenticação | JWT Bearer (token emitido pelo UsersAPI) | — |
| Documentação | Swagger / Swashbuckle | 10.1.6 |
| Mensageria | MassTransit + RabbitMQ | 8.1.3 (última versão MIT) |
| Logging | Serilog | 4.x |
| Testes unitários | xUnit + Moq + FluentAssertions + Bogus + AutoBogus | — |
| Testes de integração | Testcontainers.PostgreSql + Microsoft.AspNetCore.Mvc.Testing | — |
| Testes BDD | Reqnroll.xUnit | 2.4.2 |

> **Nota:** `CatalogAPI` apenas **valida** o JWT emitido pelo `UsersAPI`. Ambos compartilham a mesma `SecretKey` via variável de ambiente / Kubernetes Secret.

> **Nota:** MassTransit 8.1.3 é a última versão com licença MIT. A partir da 9.x é necessária licença comercial.

---

## Domínio

### Game

| Campo | Tipo | Descrição |
|---|---|---|
| `Id` | `Guid` | Identificador único (gerado na criação) |
| `Title` | `string` (máx. 200) | Título do jogo — obrigatório |
| `Description` | `string` | Descrição |
| `Price` | `decimal` | Preço (não negativo) |
| `Genre` | `string` (máx. 100) | Gênero |
| `Slug` | `string` (máx. 200) | Gerado automaticamente a partir do título |
| `IsActive` | `bool` | Disponibilidade no catálogo (padrão: `true`) |
| `CreatedAt` | `DateTime` | Data de cadastro (UTC) |

### Acquisition

| Campo | Tipo | Descrição |
|---|---|---|
| `Id` | `Guid` | Identificador único |
| `UserId` | `Guid` | Referência ao usuário (sem FK para User — domínio separado) |
| `GameId` | `Guid` | FK para `Game` |
| `PricePaid` | `decimal(18,2)` | Preço no momento da compra (histórico) |
| `AcquisitionDate` | `DateTime` | Data da aquisição (UTC) |

---

## Endpoints

Todas as respostas seguem o envelope padronizado `ApiResponse<T>`:

```json
{
  "success": true,
  "data": { },
  "errors": []
}
```

### `/api/v1/games`

| Método | Rota | Perfil | Status de sucesso | Descrição |
|---|---|---|---|---|
| `GET` | `/api/v1/games` | User / Admin | `200 OK` | Lista todos os jogos |
| `GET` | `/api/v1/games/search` | User / Admin | `200 OK` | Busca com filtros (título, gênero, preço máximo, paginação) |
| `POST` | `/api/v1/games` | Admin | `201 Created` | Cadastra novo jogo |
| `PATCH` | `/api/v1/games/{id}` | Admin | `200 OK` | Atualiza parcialmente um jogo |
| `DELETE` | `/api/v1/games/{id}` | Admin | `200 OK` | Remove jogo |
| `POST` | `/api/v1/games/{id}/acquire` | User / Admin | `202 Accepted` | Inicia compra — publica `OrderPlacedEvent` |

### Parâmetros de filtro — `GET /search`

| Parâmetro | Tipo | Descrição |
|---|---|---|
| `title` | `string` | Filtro parcial por título |
| `genre` | `string` | Filtro exato por gênero |
| `price` | `decimal` | Preço máximo |
| `page` | `int` | Página (padrão: 1) |
| `pageSize` | `int` | Itens por página (padrão: 20) |

### Políticas de autorização

| Política | Requisito |
|---|---|
| `AdminOnly` | Role = `Admin` |
| `UserOrAdmin` | Role = `User` ou `Admin` |

---

## Eventos Publicados e Consumidos

### OrderPlacedEvent (publicado)

Publicado após `POST /api/v1/games/{id}/acquire`. Consumido pelo `PaymentsAPI`.

```csharp
public record OrderPlacedEvent
{
    public Guid OrderId    { get; init; }
    public Guid UserId     { get; init; }
    public Guid GameId     { get; init; }
    public string GameTitle { get; init; }
    public decimal Price   { get; init; }
    public DateTime PlacedAt { get; init; }
}
```

### PaymentProcessedEvent (consumido)

Consumido do RabbitMQ. Publicado pelo `PaymentsAPI`.

```csharp
public record PaymentProcessedEvent
{
    public Guid OrderId      { get; init; }
    public Guid UserId       { get; init; }
    public Guid GameId       { get; init; }
    public string GameTitle  { get; init; }
    public string UserEmail  { get; init; }
    public decimal Amount    { get; init; }
    public PaymentStatus Status { get; init; }  // Approved | Rejected
    public string? Reason    { get; init; }
    public DateTime ProcessedAt { get; init; }
}
```

**Comportamento do consumer:**
- `Approved` → persiste `Acquisition` via `AcquisitionService.AddToLibraryAsync()`
- `Rejected` → loga aviso; nenhuma ação adicional
- Política de retry: 3 tentativas com intervalo de 5 segundos

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — necessário para PostgreSQL e RabbitMQ

---

## Variáveis de Ambiente

Crie um arquivo `.env` na raiz do repositório (use `.env.example` como base):

```bash
cp .env.example .env
```

| Variável | Descrição | Padrão (`.env.example`) |
|---|---|---|
| `POSTGRES_PASSWORD` | Senha do PostgreSQL | `fcg_secret` |
| `JWT_SECRET` | Chave secreta do JWT — deve ser a mesma do UsersAPI | — |
| `RABBITMQ_PASSWORD` | Senha do RabbitMQ | `guest` |

As variáveis de ambiente da aplicação (injetadas pelo docker-compose):

| Variável | Descrição |
|---|---|
| `ConnectionStrings__Postgres` | Connection string do PostgreSQL |
| `Jwt__SecretKey` | Chave secreta JWT |
| `Jwt__Issuer` | Issuer do token (`FCG.UsersAPI`) |
| `Jwt__Audience` | Audience do token (`FCG.Client`) |
| `RabbitMq__Host` | Host do RabbitMQ |
| `RabbitMq__Username` | Usuário do RabbitMQ |
| `RabbitMq__Password` | Senha do RabbitMQ |

> **Segurança:** O arquivo `.env` está no `.gitignore`. Nunca o comite. Nunca suba `appsettings.Production.json` ou `k8s/secret.yaml` com valores reais.

---

## Rodando com Docker

```bash
# 1. Crie o .env com suas credenciais
cp .env.example .env

# 2. Suba todos os serviços (PostgreSQL, RabbitMQ, API)
docker compose up -d

# 3. Para reconstruir a imagem da API após alterações
docker compose up -d --build catalog-api
```

A API estará disponível em:
- **HTTP:** `http://localhost:8081`
- **Swagger UI:** `http://localhost:8081/swagger`

---

## Rodando localmente

### 1. Suba PostgreSQL e RabbitMQ

```bash
docker run -d --name postgres \
  -e POSTGRES_USER=fcg \
  -e POSTGRES_PASSWORD=fcg_secret \
  -e POSTGRES_DB=fcg_catalog_db \
  -p 5432:5432 postgres:16-alpine

docker run -d --name rabbitmq \
  -p 5672:5672 -p 15672:15672 \
  rabbitmq:3-management-alpine
```

### 2. Restaure dependências

```bash
dotnet restore
```

### 3. Execute a API

```bash
dotnet run --project src/FCG-CATALOG-API.Api/FCG-CATALOG-API.Api.csproj
```

---

## Migrations

As migrations são aplicadas automaticamente na inicialização da API (`db.Database.Migrate()` em `Startup.Configure`).

Para criar uma nova migration manualmente:

```bash
dotnet ef migrations add <NomeDaMigration> \
  --project src/FCG-CATALOG-API.Infra \
  --startup-project src/FCG-CATALOG-API.Api
```

Para aplicar manualmente:

```bash
dotnet ef database update \
  --project src/FCG-CATALOG-API.Infra \
  --startup-project src/FCG-CATALOG-API.Api
```

---

## Testes

O projeto de testes está em `src/FCG-CATALOG-API.Tests` e cobre três camadas:

### Unitários

Testam `GameService` (Application) e `GameRepository` (Infra) de forma isolada com Moq, Bogus e FluentAssertions. Sem dependências externas.

```bash
dotnet test src/FCG-CATALOG-API.Tests \
  --filter "FullyQualifiedName~Unit"
```

### BDD (Reqnroll / Gherkin)

19 cenários em português descrevendo o comportamento de `GameService`. Sem dependências externas.

```bash
dotnet test src/FCG-CATALOG-API.Tests \
  --filter "FullyQualifiedName~BDD"
```

Feature file: `src/FCG-CATALOG-API.Tests/BDD/Features/GameService.feature`

### Integração

Testam os endpoints HTTP de ponta a ponta com banco de dados real (PostgreSQL via **Testcontainers**). RabbitMQ é mockado — não precisa estar rodando.

**Pré-requisito:** Docker Desktop em execução.

```bash
dotnet test src/FCG-CATALOG-API.Tests \
  --filter "FullyQualifiedName~Integration"
```

### Todos os testes

```bash
dotnet test src/FCG-CATALOG-API.Tests
```

### Resumo por camada

| Camada | Arquivo(s) | Testes | Docker |
|---|---|---|---|
| Unit — GameService | `Unit/Application/Services/GameServiceTests.cs` | 17 | Não |
| Unit — GameRepository | `Unit/Infra/Repositories/GameRepositoryTests.cs` | 14 | Não |
| BDD | `BDD/Features/GameService.feature` + `StepDefinitions/` | 19 cenários | Não |
| Integração — Gerenciamento | `Integration/Games/GameManagementIntegrationTests.cs` | 14 | Sim |
| Integração — Aquisição | `Integration/Games/GameAcquisitionIntegrationTests.cs` | 6 | Sim |
| Integração — Segurança | `Integration/Games/GameSecurityIntegrationTests.cs` | 11 | Sim |
| Integração — Fluxo E2E | `Integration/Flows/GameStoreFlowIntegrationTests.cs` | 5 | Sim |

---

## Estrutura do Projeto

```
fcg-catalog-api/
├── src/
│   ├── FCG-CATALOG-API.Domain/
│   │   ├── Common/
│   │   │   └── Result.cs                      # Result<T> pattern
│   │   ├── Entities/
│   │   │   ├── Game.cs
│   │   │   └── Acquisition.cs
│   │   └── Interfaces/
│   │       ├── IRepositoryBase.cs
│   │       ├── IGameRepository.cs
│   │       ├── IAcquisitionRepository.cs
│   │       └── IAcquisitionService.cs
│   │
│   ├── FCG-CATALOG-API.Application/
│   │   ├── Configure/
│   │   │   └── ApplicationConfigure.cs        # DI dos serviços
│   │   ├── DTOs/
│   │   │   ├── GameDto.cs
│   │   │   ├── AcquireGameDto.cs
│   │   │   └── FiltersDto.cs
│   │   ├── Events/
│   │   │   ├── OrderPlacedEvent.cs
│   │   │   └── PaymentProcessedEvent.cs
│   │   ├── Interfaces/
│   │   │   └── IGameService.cs
│   │   └── Services/
│   │       ├── GameService.cs
│   │       └── AcquisitionService.cs
│   │
│   ├── FCG-CATALOG-API.Infra/
│   │   ├── Configure/
│   │   │   └── ConfigureInfra.cs              # DI: repos + MassTransit
│   │   ├── Consumers/
│   │   │   └── PaymentProcessedConsumer.cs
│   │   ├── Migrations/
│   │   ├── Repositories/
│   │   │   ├── RepositoryBase.cs
│   │   │   ├── GameRepository.cs
│   │   │   └── AcquisitionRepository.cs
│   │   ├── AppDbContext.cs
│   │   └── AppDbContextFactory.cs
│   │
│   ├── FCG-CATALOG-API.Api/
│   │   ├── Authorization/
│   │   │   └── Policies.cs                    # Constantes das políticas JWT
│   │   ├── Common/
│   │   │   └── ApiResponse.cs                 # Envelope de resposta
│   │   ├── Controllers/
│   │   │   ├── BaseController.cs
│   │   │   └── GamesController.cs
│   │   ├── Extensions/
│   │   │   ├── JwtExtensions.cs
│   │   │   └── AuthorizationPoliciesExtensions.cs
│   │   ├── Middlewares/
│   │   │   ├── GlobalExceptionHandlerMiddleware.cs
│   │   │   └── RequestTimingMiddleware.cs
│   │   ├── Program.cs
│   │   ├── Startup.cs
│   │   └── appsettings.json
│   │
│   └── FCG-CATALOG-API.Tests/
│       ├── Unit/
│       │   ├── Application/Services/
│       │   │   └── GameServiceTests.cs
│       │   └── Infra/Repositories/
│       │       └── GameRepositoryTests.cs
│       ├── Integration/
│       │   ├── Config/
│       │   │   └── CustomWebApplicationFactory.cs
│       │   ├── Flows/
│       │   │   └── GameStoreFlowIntegrationTests.cs
│       │   └── Games/
│       │       ├── GameAcquisitionIntegrationTests.cs
│       │       ├── GameManagementIntegrationTests.cs
│       │       └── GameSecurityIntegrationTests.cs
│       └── BDD/
│           ├── Features/
│           │   └── GameService.feature
│           └── StepDefinitions/
│               └── GameServiceSteps.cs
│
├── k8s/
│   ├── configmap.yaml
│   ├── secret.yaml                            # não comitar com valores reais
│   ├── deployment.yaml
│   └── service.yaml
│
├── docker-compose.yml
├── Dockerfile
├── .env.example                               # template seguro para commitar
├── .gitignore
└── FCG-CATALOG-API.slnx
```

---

## Logs

Os logs são gerenciados pelo **Serilog** configurado via `appsettings.json`:

| Ambiente | Destinos | Nível mínimo |
|---|---|---|
| Development | Console (com cores) | Information |
| Production | Console (JSON estruturado) | Warning |

---

## Grupo 14

Projeto desenvolvido para a disciplina **Full Stack Developer** — FIAP.
