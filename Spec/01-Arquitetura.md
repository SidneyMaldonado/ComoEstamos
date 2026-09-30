# Arquitetura

## Projetos

```
ComoEstamos (MAUI)  ──►  ComoEstamos.Core  ──►  ComoEstamos.Data
        │                                            ▲
        └────────────────────────────────────────────┘
ComoEstamos.Test  ──►  Core e Data
```

| Projeto | Responsabilidade | Não pode |
|---|---|---|
| `ComoEstamos.Data` | EF Core + SQLite, entidades (`Model/`), migrations, repositórios, `UnitOfWork` | Conter regra de negócio que envolva mais de uma entidade; referenciar Core ou MAUI |
| `ComoEstamos.Core` | Casos de uso (`UseCases/`), regras de negócio, `RegraDeNegocioException` | Usar `AppDbContext` ou `DbSet` diretamente; referenciar MAUI |
| `ComoEstamos` (MAUI) | Telas, navegação, injeção de dependência (`MauiProgram`) | Orquestrar vários repositórios; nesse caso, chamar um caso de uso |
| `ComoEstamos.Test` | Testes de repositórios, da UnitOfWork e dos casos de uso | — |

## Quem a tela chama

- **Operação que envolve uma única entidade** (cadastrar carteira, renomear categoria,
  listar contas): a tela chama o **repositório** direto (`ICarteiraRepository` etc.).
  Não crie um caso de uso que só repassa a chamada ao repositório.
- **Operação que altera mais de um repositório, ou que tem regra de negócio**: a tela
  chama um **caso de uso** do Core. Ver [03-UseCases.md](03-UseCases.md).

## Injeção de dependência

`MauiProgram` registra, nesta ordem:

```csharp
builder.Services.AddComoEstamosData(caminhoBanco); // DbContextFactory, IUnitOfWorkFactory, repositórios
builder.Services.AddComoEstamosCore();             // casos de uso
```

Repositórios, `IUnitOfWorkFactory` e casos de uso são **singletons** e não guardam estado:
cada operação abre o próprio `DbContext` ou a própria unidade de trabalho.

## Convenções gerais

- Código, nomes e mensagens em português, seguindo o que já existe (`ObterPorIdAsync`, `ListarPorUsuarioAsync`...).
- Toda interface fica no mesmo arquivo da implementação (`ICarteiraRepository` em `CarteiraRepository.cs`).
- Exclusão é sempre lógica (`DesativarAsync`). As chaves estrangeiras são `Restrict`.
- As datas de auditoria (`DataCriacao`, `DataAlteracao`) são preenchidas pelo `AppDbContext`, nunca à mão.
