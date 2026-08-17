# Ambev Developer Evaluation — Sales API

API REST para gerenciamento de vendas, implementada em **.NET 8**, com foco em DDD, Clean Architecture, SOLID, persistência com Entity Framework Core/PostgreSQL, idempotência, observabilidade e testes automatizados.

## Índice

- [Objetivo](#objetivo)
- [Funcionalidades](#funcionalidades)
- [Regras de negócio](#regras-de-negócio)
- [Arquitetura](#arquitetura)
- [Estrutura da solução](#estrutura-da-solução)
- [Tecnologias](#tecnologias)
- [Domínio](#domínio)
- [Casos de uso](#casos-de-uso)
- [Persistência](#persistência)
- [Idempotência](#idempotência)
- [Eventos](#eventos)
- [Observabilidade](#observabilidade)
- [Docker](#docker)
- [Swagger](#swagger)
- [Testes](#testes)
- [Execução](#execução)
- [Fluxos principais](#fluxos-principais)
- [Decisões importantes](#decisões-importantes)
- [Troubleshooting](#troubleshooting)

## Objetivo

Disponibilizar uma API para criação e gerenciamento do ciclo de vida de vendas, mantendo as regras de negócio no domínio e separando responsabilidades entre Domain, Application, infraestrutura e Web API.

## Funcionalidades

- Criar venda;
- consultar venda por ID;
- listar vendas;
- atualizar dados da venda;
- atualizar itens existentes;
- adicionar novos itens durante uma atualização;
- cancelar item;
- cancelar venda;
- excluir venda;
- calcular totais e descontos;
- controlar limite de quantidade por produto;
- idempotência na criação;
- TraceId/SpanId e logs estruturados;
- eventos de domínio;
- persistência em PostgreSQL.

## Regras de negócio

| Quantidade do mesmo produto | Desconto |
|---:|---:|
| 1 to 3 | 0% |
| 4 to 9 | 10% |
| 10 to 20 | 20% |
| acima de 20 | rejeitado |

Exemplos com preço unitário de R$ 100:

- 2 unidades → R$ 200,00;
- 4 unidades → R$ 360,00 após 10% de desconto;
- 10 unidades → R$ 800,00 após 20% de desconto;
- 21 unidades → operação inválida.

## Arquitetura

A solução segue uma abordagem inspirada em **Clean Architecture + DDD**, com dependências direcionadas para o núcleo de negócio.

text
                    ┌──────────────────┐
                    │     Web API       │
                    │ ASP.NET Core 8   │
                    └────────┬─────────┘
                             │
                             ▼
                    ┌──────────────────┐
                    │   Application    │
                    │ Commands/Queries │
                    │ Handlers/Valid.  │
                    └────────┬─────────┘
                             │
                             ▼
                    ┌──────────────────┐
                    │     Domain       │
                    │ Entities/Rules   │
                    │ Domain Events    │
                    └────────┬─────────┘
                             │
                             ▼
                    ┌──────────────────┐
                    │ Infrastructure   │
                    │ EF Core/Postgres │
                    └──────────────────┘


Princípios aplicados:

- Separation of Concerns;
- SOLID;
- DDD;
- Dependency Inversion;
- Clean Code;
- Repository Pattern;
- Domain Events;
- Mediator/Handlers para casos de uso;
- testes unitários e de integração.

## Estrutura da solução

text
Ambev.DeveloperEvaluation.sln
│
├── Adapters
│   ├── Driven
│   │   └── Infra
│   │       └── ORM
│   └── Drivers
│       └── WebApi
│
├── Core
│   ├── Application
│   │   └── Application
│   └── Domain
│       └── Domain
│
├── Crosscuting
│   ├── Common
│   └── IoC
│
├── Tests
│   ├── Ambev.DeveloperEvaluation.Unit
│   ├── Ambev.DeveloperEvaluation.Integration
│   └── Ambev.DeveloperEvaluation.Functional
│
└── docker-compose.yml


### Domain

Contém as regras que não dependem de HTTP, banco ou infraestrutura. Entre os principais elementos estão `Sale`, `SaleItem`, `SaleIdempotencyRecord`, políticas de desconto, exceções e eventos.

### Application

Organiza os casos de uso de Sales:

text
Sales/
├── CreateSale/
├── GetSale/
├── ListSale/
├── UpdateSale/
├── CancelSale/
├── CancelItem/
└── DeleteSale/


Cada caso de uso concentra seu Command/Query, Handler, Validator e modelos de entrada/saída conforme a implementação do projeto.

## Tecnologias

- .NET 8;
- ASP.NET Core Web API;
- C#;
- Entity Framework Core;
- PostgreSQL;
- MediatR/padrão Mediator;
- FluentValidation;
- xUnit;
- FluentAssertions;
- NSubstitute;
- Docker/Docker Compose;
- Serilog;
- OpenTelemetry;
- OpenTelemetry Collector;
- Loki;
- Grafana;
- PostgreSQL, MongoDB e Redis conforme infraestrutura do compose.

## Domínio

### Sale

É o agregado principal e controla dados da venda, seus itens, totais, cancelamento e eventos de domínio.

### SaleItem

Representa um produto dentro da venda e mantém quantidade, preço unitário, desconto, total e estado de cancelamento.

### SaleIdempotencyRecord

Registra a chave de idempotência e informações necessárias para impedir processamento duplicado.

### Política de desconto

A regra é encapsulada em uma política de domínio, evitando que Controllers, Handlers ou Repositories tenham que conhecer a fórmula do desconto.

## Casos de uso

### CreateSale

text
POST /Sales
   │
   ▼
CreateSaleCommand
   │
   ▼
Validator
   │
   ▼
CreateSaleHandler
   ├── idempotência
   ├── cria agregado
   ├── aplica regras
   ├── persiste
   └── publica SaleCreated


### UpdateSale

O update permite alterar itens existentes e adicionar novos itens.

text
PUT /Sales/{id}
   │
   ├── item existente → UPDATE
   │
   └── item novo      → INSERT


Também é possível enviar os dois no mesmo request.

### CancelItem / CancelSale / DeleteSale

Cada operação possui seu próprio Handler e Validator, mantendo os casos de uso isolados e testáveis.

## Persistência

A persistência relacional utiliza PostgreSQL através do Entity Framework Core.

A infraestrutura concentra:

- DbContext;
- Entity Configurations;
- Repositories;
- migrations;
- persistência da idempotência.

### Application-Generated IDs

Os IDs dos itens são gerados pela aplicação com `Guid`. Por isso, na configuração do EF Core, a chave deve ser tratada como não gerada pelo banco:

csharp
builder.Property(x => x.Id)
    .HasColumnType("uuid")
    .ValueGeneratedNever();


No update misto, o ChangeTracker esperado é:

text
SaleItem existente → Modified
SaleItem novo       → Added


Isso permite que o `SaveChangesAsync` execute `UPDATE` e `INSERT` corretamente.

## Idempotência

A criação utiliza uma chave enviada no header:

http
Idempotency-Key: swagger-create-001


A mesma chave com o mesmo payload deve retornar o resultado já processado, sem criar outra venda.

A mesma chave associada a um payload diferente deve ser tratada como conflito.

## Eventos

Os eventos representam fatos importantes do domínio:

text
SaleCreated
SaleModified
SaleCancelled
ItemCancelled


A aplicação utiliza uma abstração de publisher, mantendo o domínio desacoplado do mecanismo de transporte.

Uma evolução natural é conectar esse publisher a RabbitMQ ou Kafka sem alterar as regras de negócio.

## Observabilidade

A aplicação utiliza logs estruturados e correlação por TraceId/SpanId.

text
.NET 8 API
    │
    ├── ILogger / Serilog
    ├── TraceId
    └── SpanId
          │
          ▼
OpenTelemetry
          │
          ▼
OTel Collector
          │
          ▼
        Loki
          │
          ▼
       Grafana


Exemplo de log:

csharp
_logger.LogInformation(
    "Sale created successfully. SaleId={SaleId}, SaleNumber={SaleNumber}",
    sale.Id,
    sale.SaleNumber);


O uso de propriedades estruturadas facilita busca e correlação.

### TraceId

O TraceId permite acompanhar os logs de uma mesma requisição. No Grafana/Loki, uma consulta pode ser feita por:

logql
{trace_id="SEU-TRACE-ID"}


ou, restringindo ao serviço:

logql
{service_name="ambev-sales-api", trace_id="SEU-TRACE-ID"}


Também é possível pesquisar mensagens:

logql
{service_name="ambev-sales-api"} |= "Sale created successfully"


## Docker

O `docker-compose.yml` reúne a aplicação e as dependências utilizadas pelo projeto, incluindo serviços de banco/cache e a infraestrutura de observabilidade.

Subir ambiente:

bash
docker compose -f docker-compose.yml --profile essentials up -d

É necessário passar o `--profile`. Atualmente há:
- app: WebApi + utilitários;
- debug: utilitários;
- essentials: essenciais para o projeto desenvolvido no teste técnico;

Ver status:

bash
docker compose ps

Derrubar:

bash
docker compose -f docker-compose.yml --profile essentials down -v


## Swagger

Com a Web API em execução, abra a URL do Swagger configurada pelo projeto, normalmente:

text
http://localhost:<porta>/swagger


O Swagger permite testar o CRUD e os fluxos de cancelamento.

O arquivo em docs\`sales-swagger-test-flows.json` contém exemplos de payload para:

- criação sem desconto;
- desconto de 10%;
- desconto de 20%;
- idempotência;
- GET de lista e detalhe;
- update de item existente;
- inclusão de item novo;
- update + inclusão no mesmo request;
- cancelamento de item;
- cancelamento de venda;
- delete.

## Testes

A solução possui testes unitários, de integração e funcionais conforme os projetos presentes na solução.

### Unitários — Domain

text
Domain/Entities/
├── SaleEntityTests.cs
├── SaleItemEntityTests.cs
└── SaleIdempotencyRecordTests.cs


São cobertos cenários como:

- criação e validação de Sale;
- manipulação de itens;
- cálculo de total;
- descontos;
- cancelamento;
- eventos;
- limite de 20 unidades;
- idempotency record.

Um importante teste de boundary é:

text
18 + 2 = 20 → permitido
18 + 3 = 21 → rejeitado e estado preservado


### Unitários — Application/Sales

Os casos de uso de Sales possuem testes para Handlers e Validators:

text
Application/Sales/
├── CancelItem/
├── CancelSale/
├── CreateSale/
├── DeleteSale/
├── GetSale/
├── ListSale/
└── UpdateSale/


Entre os cenários cobertos:

- sucesso;
- validação de entrada;
- entidades inexistentes;
- venda cancelada;
- SaleNumber duplicado;
- idempotência;
- publicação de eventos;
- atualização de item existente;
- inclusão de item novo;
- update de item existente + inclusão de novo item.

### Integração

O projeto de integração valida o comportamento da API e da persistência, incluindo cenários de CRUD, idempotência e atualização de itens.

Exemplo de execução de um projeto específico:

bash
dotnet test tests/Ambev.DeveloperEvaluation.Integration


### Comandos gerais

bash
dotnet restore .\Ambev.DeveloperEvaluation.sln
dotnet build .\Ambev.DeveloperEvaluation.sln
dotnet test .\Ambev.DeveloperEvaluation.sln
docker compose -f docker-compose.yml --profile essentials up -d
dotnet ef database update --project src/Ambev.DeveloperEvaluation.ORM --startup-project src/Ambev.DeveloperEvaluation.WebApi
docker compose -f docker-compose.yml  --profile essentials down -v


## Fluxos principais

### Create Sale

text
Client
  │
  │ POST /Sales
  ▼
Web API
  │
  ▼
CreateSaleHandler
  ├── Validator
  ├── Idempotência
  ├── Sale Aggregate
  ├── Repository
  └── SaleCreated


### Update Sale

text
Client
  │
  │ PUT /Sales/{id}
  ▼
UpdateSaleHandler
  ├── Header
  ├── Item existente → Modified
  ├── Item novo       → Added
  ├── Repository
  └── SaleModified


### Cancel Item

text
CancelItemHandler
  ├── carrega Sale
  ├── cancela item
  ├── persiste
  └── ItemCancelled


### Cancel Sale

text
CancelSaleHandler
  ├── carrega Sale
  ├── cancela venda
  ├── persiste
  └── SaleCancelled


## Decisões importantes

### Domain Rules

Desconto, limite de quantidade, cancelamento e consistência dos itens pertencem ao domínio. Controllers e repositories não devem duplicar essas regras.

### Application-Generated IDs

O `Guid` é gerado pela aplicação e o EF utiliza `ValueGeneratedNever`. Isso é especialmente importante quando uma atualização combina itens existentes e novos.

### Structured Logging

Exemplo dos logs:

csharp
_logger.LogInformation(
    "Sale created successfully. SaleId={SaleId}, SaleNumber={SaleNumber}",
    sale.Id,
    sale.SaleNumber);

Assim os campos continuam disponíveis para ferramentas de observabilidade.

### Events de atualização

Uma operação de `UpdateSale` deve representar uma única mudança de alto nível. Métodos internos de alteração de header/item não devem gerar eventos duplicados da mesma operação. O teste do Handler deve garantir a quantidade esperada de `SaleModifiedEvent`.

## Troubleshooting

### PostgreSQL e portas

Diferencie a porta interna do container da porta publicada no host. Uma aplicação executando dentro do Docker normalmente utiliza o nome do serviço e a porta interna; ferramentas executadas no host utilizam `localhost` e a porta publicada.

### Migration não aparece

Liste as migrations:

bash
dotnet ef migrations list


Em ambientes descartáveis, recrie o banco/container conforme a estratégia de desenvolvimento do projeto.

### `DbUpdateConcurrencyException` ao adicionar item

Quando o ID é gerado pela aplicação, confirme:

csharp
.ValueGeneratedNever()


E confirme o tracking:

text
existente → Modified
novo      → Added


Um item novo marcado como `Modified` fará o EF executar `UPDATE` de uma linha que ainda não existe, resultando em `0 rows affected`.

### TraceId não aparece no Grafana

Valide a cadeia completa:

1. API gerando TraceId;
2. logs chegando ao Serilog/ILogger;
3. OTel Collector em execução;
4. Loki recebendo os logs;
5. datasource Loki configurado no Grafana;
6. `trace_id` presente nos registros.

Comece com uma consulta ampla:

logql
{service_name="ambev-sales-api"}


Depois filtre por mensagem e, por fim, por TraceId.

---

## Licença / contexto

Projeto desenvolvido no contexto de avaliação técnica **Ambev Developer Evaluation**.
