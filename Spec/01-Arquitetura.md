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
| `ComoEstamos.Core` | Casos de uso (`UseCases/`), services (`Services/`), usuário atual, validação de domínio (`Validacao/`), formato pt-BR (`Formatacao/`), `RegraDeNegocioException` | Usar `AppDbContext` ou `DbSet` diretamente; referenciar MAUI |
| `ComoEstamos` (MAUI) | Telas, navegação, injeção de dependência (`MauiProgram`). Ver [05-Telas.md](05-Telas.md) | Chamar repositório ou caso de uso diretamente; a tela só fala com o service |
| `ComoEstamos.Test` | Testes de repositórios, da UnitOfWork e dos casos de uso | — |

## Quem a tela chama

Decisão de 07/10/2026 (substitui a regra anterior de "CRUD simples vai direto ao repositório"):
**toda** operação segue o mesmo caminho, mesmo quando mexe em uma tabela só.

```
Página  ──►  Service  ──►  Caso de uso  ──►  Repositório  ──►  Model
```

- A página chama só o **service** da entidade (`IContaService`...), que informa o usuário atual.
- O service chama um **caso de uso** por operação. O CRUD tem 5: incluir, alterar, excluir,
  buscar todos e buscar por Id. Ver [07-Crud.md](07-Crud.md).
- O caso de uso valida o domínio e as regras e usa os **repositórios** pela unidade de trabalho
  ([02-UnitOfWork.md](02-UnitOfWork.md)). Um repositório por entidade.

## Injeção de dependência

`MauiProgram` registra, nesta ordem:

```csharp
builder.Services.AddComoEstamosData(caminhoBanco); // DbContextFactory, IUnitOfWorkFactory, repositórios
builder.Services.AddComoEstamosCore();             // casos de uso, services e usuário atual
```

Repositórios, `IUnitOfWorkFactory`, casos de uso e services são **singletons** e não guardam estado:
cada operação abre o próprio `DbContext` ou a própria unidade de trabalho.

## Convenções gerais

- Código, nomes e mensagens em português, seguindo o que já existe (`ObterPorIdAsync`, `ListarPorUsuarioAsync`...).
- Toda interface fica no mesmo arquivo da implementação (`ICarteiraRepository` em `CarteiraRepository.cs`).
- Exclusão é sempre lógica (`DesativarAsync`). As chaves estrangeiras são `Restrict`.
- As datas de auditoria (`DataCriacao`, `DataAlteracao`) são preenchidas pelo `AppDbContext`, nunca à mão.
