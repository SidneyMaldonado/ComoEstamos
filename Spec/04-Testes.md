# Testes

Projeto `ComoEstamos.Test` (MSTest 4, paralelismo por método, configurado em `MSTestSettings.cs`).

## Organização

| Pasta | O que testa |
|---|---|
| `RepositoryTests/` | Um arquivo `<Nome>RepositoryTests.cs` por repositório, mais `RepositoryBaseTests` e `UnitOfWorkTests` |
| `UseCaseTests/` | Um arquivo `<Nome>UseCaseTests.cs` por caso de uso |

## Regras

- **Banco:** EF Core **InMemory** (`Microsoft.EntityFrameworkCore.InMemory`). **Não use fakes nem mocks**
  de repositório ou de `DbContext`: os testes exercitam as classes reais.
- **Base:** toda classe de teste herda `RepositoryTestBase`, que cria um banco novo e isolado
  por teste (nome com `Guid`), já que os testes rodam em paralelo.
  - `ContextFactory`: a factory real para construir repositórios (`new XRepository(ContextFactory)`)
    e a unidade de trabalho (`new UnitOfWorkFactory(ContextFactory)`).
  - `SemearAsync(...)`: grava os dados de preparação direto no `AppDbContext`, sem passar pelo código testado.
  - `CriarContexto()`: para conferir o que ficou gravado no banco.
- **Nome do teste:** `Metodo_Comportamento` (ex.: `ExecutarAsync_RecusaVendaMaiorQueADisponivelSemGravarNada`).
- **Casos de uso:** para cada regra que recusa a operação, confira também que **nada foi gravado**.

## Limitações do InMemory

O provider InMemory **não** aplica chaves estrangeiras, o índice único de e-mail nem as check
constraints (`dia_vencimento`, `nr_valor`), e não tem transações. Por isso:

- não escreva testes que dependam dessas constraints; valide a regra no caso de uso;
- a atomicidade da gravação é garantida pelo `SaveChanges` único de `ConfirmarAsync`, que o
  InMemory também respeita. Os testes de `UnitOfWorkTests` cobrem esse ponto.

Se for preciso testar comportamento específico do SQLite, a alternativa é o SQLite em memória
(`Data Source=:memory:` com a conexão aberta durante o teste). Essa mudança deve ser registrada aqui.
