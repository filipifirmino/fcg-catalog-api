# FCG Catalog API — Grupo 14

Microsserviço responsável pelo CRUD de jogos e orquestração do fluxo de compra da plataforma FIAP Cloud Games (FCG). Migrado do monolito `FCG_GRUPO_14` como parte da Fase 2 — migração para microsserviços.

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
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Logs](#logs)

---

## Responsabilidade

| Item | Detalhe |
|---|---|
| Domínio | Catalog (Games + Acquisitions) |
| Tipo | Web API (HTTP) |
| Porta | 8081 |
| Banco de dados | PostgreSQL — `fcg_catalog_db` |
| Publica evento | `OrderPlacedEvent` → RabbitMQ |
| Consome evento | `PaymentProcessedEvent` ← RabbitMQ |

---

## Arquitetura

O serviço adota **Clean Architecture**, organizando o código em camadas com dependências que fluem sempre de fora para dentro.

```
fcg-catalog-api/
└── src/
    ├── FCG.Catalog.Domain        # Entidades, interfaces
    ├── FCG.Catalog.Application   # Casos de uso: GameService, AcquisitionService
    ├── FCG.Catalog.Infra         # EF Core, repositórios, migrations
    └── FCG.Catalog.Api           # Controllers, middlewares, publishers, consumers
```

**Fluxo de dependências:**
```
FCG.Catalog.Api  →  FCG.Catalog.Application  →  FCG.Catalog.Domain
FCG.Catalog.Infra  →  FCG.Catalog.Application  →  FCG.Catalog.Domain
```

`FCG.Catalog.Domain` não depende de nenhum outro projeto da solução.

### Padrões aplicados

- **Repository Pattern** — abstração de acesso a dados via interfaces no domínio
- **DTOs** — separam o contrato HTTP das entidades de domínio
- **Middleware** — tratamento centralizado de exceções
- **Event-Driven Purchase Flow** — `POST /acquire` não persiste diretamente; publica `OrderPlacedEvent` e aguarda `PaymentProcessedEvent` para confirmar a aquisição

### Fluxo de compra refatorado

```
Antes (monolito):  POST /acquire-game → GameService.AcquireGame() → salva Acquisition → retorna 200
Agora (microsserviço):
  1. POST /api/v1/games/{id}/acquire → publica OrderPlacedEvent  → retorna 202 Accepted
  2. PaymentProcessedConsumer.Consume()
       ├── Status = Approved  → AcquisitionService.AddToLibrary()  → persiste Acquisition
       └── Status = Rejected  → loga rejeição
```

---

## Tecnologias

| Camada | Tecnologia | Versão |
|---|---|---|
| Runtime | .NET | 10.0 |
| Framework Web | ASP.NET Core Web API | 10.0 |
| ORM | Entity Framework Core + Npgsql | 10.0.5 |
| Banco de Dados | PostgreSQL | 16 |
| Autenticação | JWT Bearer (token emitido pelo UsersAPI) | — |
| Documentação | Swagger / Swashbuckle | 10.1.6 |
| Mensageria | MassTransit + RabbitMQ | — |
| Logging | Serilog | 10.0.0 |

> **Nota:** CatalogAPI valida o JWT emitido pelo UsersAPI. Ambos compartilham a mesma `SecretKey` via Kubernetes Secret / variável de ambiente.

---

## Domínio

### Entidade Game

| Campo | Tipo | Descrição |
|---|---|---|
| `Id` | Guid | Identificador único |
| `Title` | string (máx. 200) | Título do jogo |
| `Description` | string | Descrição |
| `Price` | decimal | Preço atual (não negativo) |
| `Genre` | string (máx. 100) | Gênero |
| `Slug` | string (máx. 200, único) | Identificador amigável para URL |
| `IsActive` | bool | Disponibilidade no catálogo |
| `CreatedAt` | DateTime | Data de cadastro |

### Entidade Acquisition

| Campo | Tipo | Descrição |
|---|---|---|
| `Id` | Guid | Identificador único |
| `UserId` | Guid | Referência ao usuário |
| `GameId` | Guid | Referência ao jogo |
| `PricePaid` | decimal (18,2) | Preço no momento da compra (histórico) |
| `AcquisitionDate` | DateTime | Data da aquisição |

---

## Endpoints

Todas as respostas seguem o envelope padronizado `ApiResponse<T>` com os campos `data`, `success` e `errors`.

### Jogos — `/api/v1/games`

| Método | Rota | Auth | Perfil | Descrição |
|---|---|---|---|---|
| `GET` | `/api/v1/games` | Sim | User / Admin | Lista todos os jogos |
| `GET` | `/api/v1/games/search` | Sim | User / Admin | Busca com filtros (título, gênero, preço, paginação) |
| `POST` | `/api/v1/games` | Sim | Admin | Cadastra novo jogo |
| `PATCH` | `/api/v1/games/{id}` | Sim | Admin | Atualiza dados do jogo |
| `DELETE` | `/api/v1/games/{id}` | Sim | Admin | Remove jogo por ID |
| `POST` | `/api/v1/games/{id}/acquire` | Sim | User / Admin | Inicia compra — publica `OrderPlacedEvent` — retorna `202 Accepted` |

### Políticas de autorização

| Política | Requisito |
|---|---|
| `AdminOnly` | Role = Admin |
| `UserOrAdmin` | Role = User ou Admin |

---

## Eventos Publicados e Consumidos

### OrderPlacedEvent (publicado)

Publicado no exchange `order.placed` após chamada a `POST /api/v1/games/{id}/acquire`.

```csharp
public record OrderPlacedEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public string GameTitle { get; init; }
    public decimal Price { get; init; }
    public DateTime PlacedAt { get; init; }
}
```

**Consumer:** PaymentsAPI

---

### PaymentProcessedEvent (consumido)

Consumido do exchange `payment.processed`. Se `Status = Approved`, o jogo é adicionado à biblioteca do usuário.

```csharp
public record PaymentProcessedEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public string GameTitle { get; init; }
    public string UserEmail { get; init; }
    public decimal Amount { get; init; }
    public PaymentStatus Status { get; init; }  // Approved | Rejected
    public string? Reason { get; init; }
    public DateTime ProcessedAt { get; init; }
}
```

**Publisher:** PaymentsAPI

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — PostgreSQL + RabbitMQ via Docker Compose (repositório `fcg-infra`)

---

## Variáveis de Ambiente

| Variável | Descrição | Padrão (dev) |
|---|---|---|
| `ConnectionStrings__Postgres` | Connection string do PostgreSQL | `Host=postgres;Port=5432;Database=fcg_catalog_db;Username=fcg;Password=fcg_secret` |
| `Jwt__SecretKey` | Chave secreta do JWT (mesma do UsersAPI) | — |
| `Jwt__Issuer` | Issuer do token | `FCG.UsersAPI` |
| `Jwt__Audience` | Audience do token | `FCG.Client` |
| `RabbitMq__Host` | Host do RabbitMQ | `rabbitmq` |
| `RabbitMq__Username` | Usuário do RabbitMQ | `guest` |
| `RabbitMq__Password` | Senha do RabbitMQ | `guest` |

---

## Rodando com Docker

Suba toda a infraestrutura a partir do repositório `fcg-infra`:

```bash
docker compose up -d
```

A API estará disponível em:
- **HTTP:** `http://localhost:8081`
- **Swagger UI:** `http://localhost:8081/swagger`

---

## Rodando localmente

### 1. Configure o PostgreSQL e RabbitMQ

```bash
docker run -d --name postgres -e POSTGRES_USER=fcg -e POSTGRES_PASSWORD=fcg_secret -e POSTGRES_DB=fcg_catalog_db -p 5432:5432 postgres:16-alpine
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management-alpine
```

### 2. Restaure dependências e aplique migrations

```bash
dotnet restore
dotnet ef database update --project src/FCG.Catalog.Infra --startup-project src/FCG.Catalog.Api
```

### 3. Execute a API

```bash
dotnet run --project src/FCG.Catalog.Api/FCG.Catalog.Api.csproj
```

---

## Estrutura do Projeto

```
src/
├── FCG.Catalog.Api/
│   ├── Controllers/                 # GamesController
│   ├── Extensions/                  # JwtExtensions, SwaggerExtensions
│   ├── Middlewares/                 # GlobalExceptionHandler
│   ├── Events/
│   │   ├── Publishers/              # OrderPlacedEventPublisher
│   │   └── Consumers/               # PaymentProcessedConsumer
│   └── appsettings.json
│
├── FCG.Catalog.Application/
│   ├── Services/                    # GameService, AcquisitionService
│   ├── DTOs/                        # Modelos de request e response
│   └── Interfaces/                  # IGameService, IAcquisitionService
│
├── FCG.Catalog.Domain/
│   ├── Entities/                    # Game, Acquisition
│   └── Interfaces/                  # IGameRepository, IAcquisitionRepository
│
└── FCG.Catalog.Infra/
    ├── Repositories/                # GameRepository, AcquisitionRepository
    ├── Mappings/                    # Configurações EF Core (Fluent API)
    ├── Migrations/                  # Histórico de migrations
    └── AppDbContext.cs
```

---

## Logs

Os logs são gerenciados pelo **Serilog** com comportamento diferente por ambiente:

| Ambiente | Destinos | Nível mínimo |
|---|---|---|
| Development | Console + arquivo `Logs/log-dev-<hora>.txt` | Debug |
| Production | Arquivo `Logs/log-prod-<data>.txt` (JSON) | Information |

---

## Grupo 14

Projeto desenvolvido para a disciplina **Full Stack Developer** — FIAP.
