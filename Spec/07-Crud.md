# Estrutura de CRUD

Decisão de 07/10/2026: todo CRUD segue a mesma estrutura em camadas, mesmo quando a operação
mexe em uma tabela só. A página nunca chama repositório nem caso de uso diretamente.

```
Página (MAUI)  ──►  Service (Core)  ──►  Caso de uso (Core)  ──►  Repositório (Data)  ──►  Model
 ListarXsPage        IXService            IncluirXUseCase           IXRepository            X
 IncluirXPage                             AlterarXUseCase
 AlterarXPage                             ExcluirXUseCase
 VisualizarXPage                          BuscarTodosXsUseCase
                                          BuscarXPorIdUseCase
```

| Camada | Responsabilidade | Não faz |
|---|---|---|
| Página | Lê e mostra os campos, converte texto em número/data (erro de **formato**), navega, confirma a exclusão | Regra de negócio, acesso a repositório |
| Service | Fachada da entidade para as páginas: um método por caso de uso; informa o **usuário atual** | Validação, acesso a repositório |
| Caso de uso | Valida o **domínio** dos campos e as regras, confere o dono do registro, grava pela unidade de trabalho | Formatação para a tela |
| Repositório | Uma entidade, uma tabela. Consulta e grava (ver [02-UnitOfWork.md](02-UnitOfWork.md)) | Regra que envolva outra entidade |

## Checklist de uma entidade `X`

Exemplo de referência: **Conta**. Copie os arquivos dela e troque o nome.

**Data** (`ComoEstamos.Data`)

- [ ] `Model/X.cs` e `Repository/XRepository.cs` (com `ListarPorUsuarioAsync`), registrados em
  `AddComoEstamosData` e em `IUnitOfWork`.
- [ ] Métodos `ExisteAtivo...Por<X>Async` nos repositórios das entidades que **dependem** de `X`
  (usados na exclusão), com testes em `RepositoryTests/`.

**Core** (`ComoEstamos.Core`)

| Arquivo | Conteúdo |
|---|---|
| `UseCases/Xs/XValidador.cs` | `internal static`. Normaliza (remove espaços, texto vazio vira `null`) e valida o domínio de todos os campos; lança `ValidacaoException` com **todos** os erros |
| `UseCases/Xs/IncluirXUseCase.cs` | `Task<X> ExecutarAsync(X x, int idUsuario)` |
| `UseCases/Xs/AlterarXUseCase.cs` | `Task ExecutarAsync(X x, int idUsuario)` |
| `UseCases/Xs/ExcluirXUseCase.cs` | `Task ExecutarAsync(int id, int idUsuario)` |
| `UseCases/Xs/BuscarTodosXsUseCase.cs` | `Task<List<X>> ExecutarAsync(int idUsuario)` |
| `UseCases/Xs/BuscarXPorIdUseCase.cs` | `Task<X?> ExecutarAsync(int id, int idUsuario)` |
| `Services/XService.cs` | `IXService`: `BuscarTodosAsync`, `BuscarPorIdAsync(id)`, `IncluirAsync(x)`, `AlterarAsync(x)`, `ExcluirAsync(id)` |

- Concordância no nome: `BuscarTodasContasUseCase` / `BuscarTodosCredoresUseCase`; no service,
  `BuscarTodasAsync` / `BuscarTodosAsync`.
- Registro em `AddComoEstamosCore`: os 5 casos de uso e o service, todos singleton.

**Testes** (`ComoEstamos.Test`)

- [ ] `UseCaseTests/Crud<X>Tests.cs`: os 5 casos de uso (ver "Testes obrigatórios" abaixo).
- [ ] `ServiceTests/<X>ServiceTests.cs`: o service usa o usuário atual.

**MAUI** (`ComoEstamos`)

| Arquivo | Página |
|---|---|
| `Pages/Xs/ListarXsPage.xaml(.cs)` | Listar. Item do menu lateral (rota `xs`) |
| `Pages/Xs/IncluirXPage.xaml(.cs)` | Incluir. `Rota = "incluir-x"` |
| `Pages/Xs/AlterarXPage.xaml(.cs)` | Alterar. `Rota = "alterar-x"`, recebe `?id=` |
| `Pages/Xs/VisualizarXPage.xaml(.cs)` | Visualizar. `Rota = "visualizar-x"`, recebe `?id=` |

- [ ] As 4 páginas como `AddTransient` em `MauiProgram`; as rotas de Incluir, Alterar e Visualizar em
  `AppShell.xaml.cs`; o `FlyoutItem` em `AppShell.xaml`; o ícone `Resources/Images/x_icon.svg`.
- [ ] `Spec/05-Telas.md`: tabela "X: regras do formulário".
- [ ] Compilar sem avisos, rodar os testes e testar no app: incluir, validar, visualizar, alterar,
  cancelar, excluir (Sim e Não), exclusão impedida e menu.

## Casos de uso do CRUD

Valem as convenções de [03-UseCases.md](03-UseCases.md): uma unidade de trabalho por execução e
validar tudo antes de alterar. As consultas (`BuscarTodos`, `BuscarPorId`) também abrem uma unidade de
trabalho, mas não chamam `ConfirmarAsync`.

### Dono do registro

O `idUsuario` vem do service (usuário atual). O caso de uso **nunca confia** no `IdUsuario` que
vem dentro da entidade:

- **Incluir** sobrescreve `x.IdUsuario = idUsuario` e zera `x.Id`.
- **Alterar, Excluir e BuscarPorId** conferem se o registro gravado é do usuário. Registro de outro
  usuário é tratado como **inexistente**.
- Entidade sem `id_usuario` (ex.: Investimento) confere o dono pela entidade pai (`Carteira.IdUsuario`).

### Regras de cada operação

| Caso de uso | Regras |
|---|---|
| Incluir | Valida o domínio (`XValidador`) e insere ativo |
| Alterar | Inexistente ou de outro usuário → `KeyNotFoundException`. Inativo → `RegraDeNegocioException`. Valida o domínio e copia **só os campos editáveis** para a instância rastreada (`uow.Xs.ObterPorIdAsync`); campos fora do formulário (ex.: `img_`) não são perdidos |
| Excluir | Mesmas verificações do Alterar. Se houver **dependentes ativos**, recusa com `RegraDeNegocioException` ("Não é possível excluir: existem ... ativos neste(a) ..."). Senão, `DesativarAsync` (exclusão lógica) |
| BuscarTodos | Só os ativos do usuário, na ordem do repositório (por nome) |
| BuscarPorId | `null` se não existe, se é de outro usuário ou se está inativo |

### Validação de domínio

Todo campo do formulário tem o domínio validado **no caso de uso**, a partir da definição da coluna
no `Model` (`MaxLength`, `Precision`, nulidade) e das regras da entidade:

| Tipo da coluna | Regra |
|---|---|
| Texto obrigatório (`nm_`) | Remove espaços nas pontas; não pode ficar vazio; até o `MaxLength` |
| Texto opcional (`ds_`) | Remove espaços nas pontas; vazio vira `null`; até o `MaxLength` (se houver) |
| Número `decimal(p,s)` | No máximo `s` casas decimais; valor absoluto até o maior que cabe em `p` dígitos (`Limites.Decimal10x2`, `Limites.Decimal18x6`); mínimo conforme a regra (ex.: quantidade ≥ 0) |
| Chave estrangeira (`id_`) | Obrigatória se `NOT NULL`; o registro deve existir, estar ativo e ser do mesmo usuário |
| Nome | **Único entre os ativos** do mesmo usuário (Investimento: da mesma carteira), sem diferenciar maiúsculas de minúsculas |

- Os erros são acumulados em `ValidadorCampos` (`Core/Validacao`), **um por campo** (o primeiro), e
  lançados juntos em uma `ValidacaoException`, que herda de `RegraDeNegocioException`.
- Cada erro tem o **nome da propriedade** (`nameof(Conta.Nome)`) e a mensagem pronta para a tela.
- A página mostra cada mensagem abaixo do campo correspondente. Erro de um campo que não está na
  página aparece num alerta.

### Erros de formato (na página)

Converter o texto digitado é papel da página, com `FormatoBr.TentarLerDecimal` (`Core/Formatacao`):

- Campo numérico vazio → "Informe ⟨o campo⟩."; texto que não é número → "⟨Campo⟩ inválido(a). Use o formato 1.250,00."
- Se houver erro de formato, a página mostra e **não chama o service**. Os erros de domínio aparecem
  quando o formato estiver certo.
- Formato aceito: brasileiro (`1.250,50`). Sem vírgula, um único ponto é decimal (`1250.5`, `0.000123`),
  exceto quando seguido de exatamente 3 dígitos e a parte inteira não é zero (`1.000` = mil).

## Páginas

Valem as convenções de [05-Telas.md](05-Telas.md) (fundo, cores, estilos de `Styles.xaml`).
As 4 páginas são **independentes**: cada uma tem o próprio XAML. O que se repete fica em estilos
(`Styles.xaml`), na tabela genérica (`Controls/TabelaRegistros`) e em `Pages/Formulario.cs`.

```
ListarXsPage ──toque na linha──► VisualizarXPage ──lápis──► AlterarXPage
     │                               │  ▲                       │
     └──"+"──► IncluirXPage          │  └───Salvar/Cancelar─────┘
                  │                  └──lixeira──► popup "Sim" ──► volta para a lista
                  └──Salvar/Cancelar──► volta para a lista
```

| Página | Topo | Conteúdo | Botões |
|---|---|---|---|
| Listar | ☰ (menu), título no plural, **+** | `TabelaRegistros` com os ativos; mensagem "Nenhum(a) ... cadastrado(a)." | — |
| Visualizar | Título "⟨X⟩", **lápis** (alterar) e **lixeira** (excluir) | Cartão só de leitura: cada campo com título e valor, mais "Data de Cadastro" e "Última Alteração" | Voltar |
| Incluir | Título "Incluir ⟨X⟩" | Formulário com a mensagem de erro abaixo de cada campo | Cancelar, Salvar |
| Alterar | Título "Alterar ⟨X⟩" | O mesmo formulário, preenchido | Cancelar, Salvar |

- Listar e Visualizar recarregam em `OnAppearing` (refletem inclusões, alterações e exclusões).
- Visualizar e Alterar: se o registro não existe mais (BuscarPorId devolve `null`), avisam e voltam.
- **Excluir não tem página**: a lixeira da Visualizar pede confirmação ("Confirma a exclusão de ...?",
  Sim/Não). Sim chama `ExcluirAsync` e volta para a lista; Não fica na Visualizar. Se a exclusão for
  recusada (dependentes), mostra a mensagem num alerta e fica na Visualizar.
- Erros ao salvar: `ValidacaoException` → mensagens nos campos; qualquer outra exceção → alerta
  "Não foi possível salvar" com a mensagem.

## Testes obrigatórios

Em `UseCaseTests/Crud<X>Tests.cs` (ver [04-Testes.md](04-Testes.md)):

- Incluir: grava ativo com o usuário recebido; remove espaços; recusa cada regra de domínio **sem gravar
  nada** (texto vazio, tamanho, casas decimais, faixa, chave estrangeira inativa ou de outro usuário);
  recusa nome repetido e aceita nome igual de **outro** usuário ou de registro **inativo**.
- Alterar: grava os campos editáveis; não perde campos fora do formulário; aceita manter o próprio nome;
  recusa nome de outro registro; inexistente e de outro usuário → `KeyNotFoundException`; inativo recusado.
- Excluir: desativa; cada dependente ativo impede (e não desativa nada); dependente inativo não impede;
  inexistente e de outro usuário → `KeyNotFoundException`.
- BuscarTodos: só ativos do usuário, em ordem. BuscarPorId: `null` para inexistente, inativo e de outro usuário.

## Entidades fora do CRUD padrão

Dívida, Operação, Parcela e Histórico **não** seguem os 5 casos de uso padrão: incluir gera ou altera
outras tabelas, e alterar/excluir dependem de decisões em aberto ([03-UseCases.md](03-UseCases.md#catálogo--candidatos-futuros)).
Elas usam esta mesma estrutura de camadas (página → service → caso de uso → repositório), com os
casos de uso específicos no lugar dos padrão.
