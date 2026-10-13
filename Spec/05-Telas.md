# Telas (MAUI)

## Organização

| Onde | O quê |
|---|---|
| `MainPage.xaml` | Tela inicial: título, tabela de contas com saldo e total no rodapé, botão "Atualizar Saldos" e o atalho para as dívidas (abaixo) |
| `Compartilhamento/ImagemSaldos.cs` | PNG da tabela de saldos compartilhada pela tela inicial (SkiaSharp) |
| `Pages/<Entidades>/` | As 4 páginas do CRUD de cada entidade (`ListarContasPage`, `VisualizarContaPage`, `IncluirContaPage`, `AlterarContaPage`). Ver [07-Crud.md](07-Crud.md) |
| `Pages/Formulario.cs` | Passos repetidos das páginas: ler o Id da rota, ler números, mostrar erros nos campos, salvar, excluir, voltar |
| `Controls/TabelaRegistros` | Tabela das listas: coluna de texto (com detalhe opcional), coluna de valor opcional e rodapé de total opcional (`Total`; nulo esconde) |
| `Resources/Styles/Styles.xaml` | Estilos com chave usados pelas páginas (ver "Visual") |
| `Resources/Styles/Colors.xaml` | Paleta do app |

- As telas usam **code-behind**, sem MVVM nem CommunityToolkit. A página recebe pelo construtor
  só os **services** do Core (`IContaService`...), nunca repositórios nem casos de uso
  ([01-Arquitetura.md](01-Arquitetura.md)).
- Toda página é registrada como `AddTransient` em `MauiProgram`.

## Usuário atual

Ainda não há login. `IUsuarioAtual` (`ComoEstamos.Core/Services/UsuarioAtual.cs`) devolve o
**usuário local padrão** (`local@comoestamos.app`) e o cria na primeira chamada se ele não existir.
As páginas não lidam com o usuário: o **service** informa o Id aos casos de uso. Quando houver login,
só o `UsuarioAtual` muda.

## Navegação

- `AppShell` tem um **menu lateral (flyout)**: Início, Contas, Dívidas, Categorias, Credores, Carteiras e Investimentos.
  Cada item de CRUD abre a página Listar (rota no plural: `contas`, `carteiras`...).
- As páginas escondem a barra de navegação (`Shell.NavBarIsVisible="False"`). As páginas do menu
  têm o ícone ☰ dourado no canto superior esquerdo, que abre o menu (`Shell.Current.FlyoutIsPresented = true`).
- Páginas fora do menu são registradas com `Routing.RegisterRoute` em `AppShell.xaml.cs`, e a página
  expõe a rota numa constante (`VisualizarContaPage.Rota = "visualizar-conta"`). Parâmetros vão na
  query (`alterar-conta?id=3`) e são lidos com `IQueryAttributable` + `Formulario.LerId`. Para voltar,
  `Formulario.VoltarAsync()` (`GoToAsync("..")`).

- **Atalho da tela inicial para as dívidas**: arrastar o dedo para cima na MainPage (`SwipeGestureRecognizer`
  `Up` no conteúdo) abre `//dividas`, como o item do menu. As linhas da tabela de saldos continuam rolando: o
  gesto vale fora delas (título, espaço abaixo da tabela, botão). A dica "︿ Arraste para cima para ver as
  dívidas", abaixo do botão "Atualizar Saldos", também abre as dívidas ao ser tocada (no Windows, o arrasto com
  o mouse não dispara o gesto).
- **Volta das dívidas para a tela inicial**: o contrário — arrastar o dedo para baixo na `ListarDividasPage` abre
  `//MainPage`. Vale fora das linhas da tabela e do campo de busca. Sem dica na tela.

## Padrão de CRUD

A estrutura completa (camadas, casos de uso, validação, fluxo entre as 4 páginas e testes) está em
[07-Crud.md](07-Crud.md). Resumo do comportamento das telas:

- **Listar**: só registros ativos, recarrega em `OnAppearing`. Tocar na linha abre a **Visualizar**; o
  **+** (sinal dourado sobre um círculo, canto superior direito) abre a **Incluir**.
- **Visualizar**: os campos só de leitura, mais "Data de Cadastro" e "Última Alteração". O **lápis**
  abre a **Alterar**; a **lixeira** pede confirmação (`DisplayAlertAsync(..., "Sim", "Não")`) e exclui
  (exclusão lógica). Botão Voltar.
- **Incluir** e **Alterar**: páginas separadas, com o mesmo formulário. Botões **Cancelar** e **Salvar**
  no fim; os dois voltam para a página anterior.
- Os títulos dos campos ficam em português correto (`nm_conta` → "Nome da Conta").
- **Validação**: cada campo errado mostra a mensagem logo abaixo dele, e nada é gravado. O formato do
  número é conferido pela página; o domínio (tamanho, faixa, casas, nome repetido...), pelo caso de uso.
- Valores em dinheiro são exibidos com `FormatoBr.Moeda` (`R$ 1.250,50`), negativos em vermelho claro.
  Na digitação, aceitam o formato brasileiro (`1.250,50`) e o ponto decimal de teclados numéricos (`1250.5`).

### Regras do formulário

Todas as entidades abaixo: o nome é obrigatório, sem espaços nas pontas e **único entre os ativos do
usuário**, sem diferenciar maiúsculas (Investimento: único na carteira). As imagens (`img_`) ainda não
aparecem nos formulários e são preservadas na alteração.

**Conta** — exclusão impedida se houver dívidas ou parcelas ativas na conta. Alterar o saldo grava
`tb_historico_saldo` ([03-UseCases.md](03-UseCases.md#histórico-de-saldo-tb_historico_saldo)).

| Campo | Título | Regra |
|---|---|---|
| `nm_conta` | Nome da Conta | Até 100 caracteres |
| `nr_saldo` | Saldo (R$) | Obrigatório, até 2 casas, entre -99.999.999,99 e 99.999.999,99; negativo é permitido |

**Atualizar Saldos** (`Pages/Contas/AtualizarSaldosPage`, rota `atualizar-saldos`, aberta pelo botão
"Atualizar Saldos" da MainPage). Uma linha por conta ativa: nome à esquerda e o saldo (mesma regra de
`nr_saldo`) à direita, já preenchido. Cancelar e Salvar no fim; o total não é recalculado na digitação.
O Salvar chama `IContaService.AtualizarSaldosAsync` (`AtualizarSaldosUseCase`):

- valida todos os saldos antes de gravar; se algum tiver erro, **nada** é gravado e a mensagem aparece
  abaixo da conta (campo `Saldo:<id>` no `ErroValidacao`);
- grava só as contas cujo saldo mudou e, para cada uma, uma linha em `tb_historico_saldo` (todas com a
  mesma data/hora), numa única unidade de trabalho;
- o histórico **não** impede a exclusão da conta (desativar o histórico junto com a conta está em
  [06-Pendencias.md](06-Pendencias.md)).

A alteração pelo CRUD de Conta (`AlterarContaUseCase`) também grava uma linha em `tb_historico_saldo`
quando o saldo muda. A inclusão da conta não grava histórico.

**Categoria** — exclusão impedida se houver dívidas ou parcelas ativas na categoria.

| Campo | Título | Regra |
|---|---|---|
| `nm_categoria` | Nome da Categoria | Até 100 caracteres |

**Credor** — exclusão impedida se houver dívidas ativas do credor.

| Campo | Título | Regra |
|---|---|---|
| `nm_credor` | Nome do Credor | Até 50 caracteres |
| `ds_observacoes` | Observações | Opcional (`Editor`), sem limite de tamanho; vazio grava `null` |

**Carteira** — exclusão impedida se houver investimentos ativos na carteira.

| Campo | Título | Regra |
|---|---|---|
| `nm_carteira` | Nome da Carteira | Até 50 caracteres |

**Investimento** — exclusão impedida se houver operações ou históricos ativos do investimento.
Lista: nome, carteira (abaixo, em letra menor) e valor (quantidade × cotação).

| Campo | Título | Regra |
|---|---|---|
| `id_carteira` | Carteira | Obrigatória (`Picker` com as carteiras ativas do usuário); pode mudar na alteração |
| `nm_investimento` | Nome do Investimento | Até 50 caracteres |
| `nr_quantidade` | Quantidade | Obrigatória, até 6 casas, de 0 a 999.999.999.999,999999 |
| `vl_cotacao` | Cotação (R$) | Obrigatória, até 6 casas, de 0 a 999.999.999.999,999999 |
| `ds_observacao` | Observação | Opcional (`Editor`), até 500 caracteres; vazio grava `null` |

## Dívidas

Fora do CRUD padrão ([07-Crud.md](07-Crud.md#entidades-fora-do-crud-padrão)): as páginas ficam em
`Pages/Dividas/` e usam o `IDividaService`. Só dívidas
(`EhDivida = true`); créditos terão tela própria. Por enquanto: Listar (consulta), Visualizar, Incluir e
Alterar; excluir é a lixeira da Visualizar. Regras de alterar e excluir em
[03-UseCases.md](03-UseCases.md#alterardividausecase).

**Listar** (`ListarDividasPage`, rota `dividas`, item "Dívidas" do menu). Regras da consulta em
[03-UseCases.md](03-UseCases.md#buscardividasusecase).

- Topo: ☰, "Dívidas" e **+** (abre a Incluir).
- Cartão de filtros, aplicados na hora (sem botão "Filtrar"):
  - **Buscar pelo nome**: parte do nome, sem diferenciar maiúsculas nem acentos;
  - **mês de vencimento**: `‹ Outubro/2026 ›`. Começa em "Todos os meses"; tocar no texto ou numa seta
    escolhe o mês atual, as setas andam um mês e o **✕** volta para "Todos os meses";
  - **Ocultar dívidas pagas**: marcada ao abrir a tela.
- Tabela: nome; abaixo, `Dia 10 · 12x de R$ 100,00` (mais `· Paga` quando paga); à direita, o valor total.
  Ordem pelo próximo vencimento em aberto, pagas no fim. Vazia: "Nenhuma dívida cadastrada." (sem filtro)
  ou "Nenhuma dívida encontrada." (com filtro).
- Tocar na linha abre a Visualizar. Os filtros continuam ao voltar.

**Visualizar** (`VisualizarDividaPage`, rota `visualizar-divida?id=`). Sem rolagem da página: em cima, o
cartão com os dados em duas colunas (Nome da Dívida; Credor e Conta; Categoria e Valor Total; Parcelas
`12x de R$ 100,00` e Situação `Paga` ou `3 de 12 pagas`; Dia de Vencimento e Primeiro Vencimento; Data de
Cadastro e Última Alteração). Embaixo, a seção **Parcelas**: tabela com `n/total · vencimento`, a situação
(`Paga em 05/10/2026`, `Vencida` ou `Em aberto`) e o valor; ela rola sozinha. Botão Voltar.
Parcelas: o valor mostrado é o da próxima parcela em aberto.

- **Lápis**: abre a Alterar.
- **Lixeira**: "Confirma a exclusão da dívida \"X\" e de todas as parcelas?" (Sim/Não). Se houver parcela paga,
  acrescenta "Os pagamentos já feitos continuam descontados do saldo da conta." Sim exclui e volta para a lista.

**Incluir** (`IncluirDividaPage`, rota `incluir-divida`). Grava pelo `CriarDividaUseCase` e gera as parcelas.

| Campo | Título | Regra |
|---|---|---|
| `nm_divida` | Nome da Dívida | Obrigatório, até 100 caracteres; pode repetir |
| `nr_valor` | Valor Total (R$) | Obrigatório, até 2 casas, de 0,01 a 99.999.999,99 |
| `nr_parcelas` | Número de Parcelas | Obrigatório, inteiro de 1 a 360; começa em 1 |
| `dt_primeiro_vencimento` | Primeiro Vencimento | `DatePicker`; começa em hoje |
| `dia_vencimento` | Dia de Vencimento | Obrigatório, de 1 a 31. Ao escolher o primeiro vencimento, recebe o dia dele (pode ser mudado) |
| `id_conta` | Conta | Obrigatória (`Picker` com as contas ativas) |
| `id_categoria` | Categoria | Obrigatória (`Picker` com as categorias ativas) |
| `id_credor` | Credor | Opcional (`Picker`; a 1ª opção é "Nenhum") |

`dm_divida` é sempre `true` nesta tela. Os inteiros são lidos com `Formulario.TentarLerInteiro`.

**Alterar** (`AlterarDividaPage`, rota `alterar-divida?id=`). O mesmo formulário da Incluir, preenchido, mais o
erro abaixo do Primeiro Vencimento. Grava pelo `AlterarDividaUseCase`. Se já houver parcelas pagas:

- o Primeiro Vencimento fica desabilitado;
- acima do formulário aparece o aviso "N de T parcelas já pagas (R$ X). Elas não mudam. Se mudar o valor, o número
  de parcelas ou o dia, só as parcelas em aberto são refeitas, com o que falta pagar. Conta e categoria novas valem
  só para as parcelas em aberto."

## Compartilhar saldos

O ícone de compartilhar (canto superior direito da MainPage, `share_icon`) gera um **PNG** (não PDF) com
as contas ativas, os saldos e o total, e abre o compartilhamento do sistema (`Share.Default`,
`ShareFileRequest`, `image/png`). O arquivo fica em `FileSystem.CacheDirectory` (`como-estamos-AAAA-MM-DD.png`).

Visual de planilha, independente do tema do app: fundo branco, grade preta; cabeçalho "Como Estamos" com
a data à direita (`14/out`) e linha "Total" em negrito sobre verde claro `#C6E0B4`; valores à direita no
formato `1.447,75`, sem "R$". Fonte Calibri quando existir (Windows); senão, a padrão do sistema.
Gerada em 3× para não borrar no celular.

## Visual

Todas as telas usam `fundo.png` (`Aspect="AspectFill"`) e os estilos com chave de `Styles.xaml`:

| Estilo | Uso |
|---|---|
| `Conteudo` / `ConteudoFormulario` | `Padding="20,50,20,20"` e espaçamento 16 (lista / páginas com rolagem) |
| `TituloPagina`, `LinhaTitulo` | Título dourado de 32 pt e a linha dourada de 2 px abaixo dele |
| `BotaoMenu` | ☰ que abre o menu |
| `BotaoCirculo` + `SinalMais` / `IconeCirculo` | Botões redondos do topo: +, lápis (`editar_icon`), lixeira (`lixeira_icon`) |
| `Cartao` | Cartão do formulário e da visualização |
| `TituloCampo`, `TituloCampoSeguinte` | Título do primeiro campo e dos seguintes (com respiro acima) |
| `Campo` (Entry), `CampoTextoLongo` (Editor), `CampoEscolha` (Picker), `CampoData` (DatePicker) | Controles de entrada |
| `CaixaMarcar` (CheckBox), `TituloSecao` | Caixa de marcar dourada dos filtros; título de seção dentro da página ("Parcelas") |
| `MensagemErro`, `ValorCampo` | Erro abaixo do campo; valor só de leitura na Visualizar |
| `BotaoPrincipal`, `BotaoSecundario` | Salvar (fundo dourado); Cancelar e Voltar (contorno dourado) |

| Cor (`Colors.xaml`) | Uso |
|---|---|
| `AzulFundo` `#1A2E6E` | Fundo da página atrás da imagem |
| `AzulEscuro` `#0A1438` | Cabeçalho de tabela, campos, círculo dos botões de ícone, menu lateral |
| `FundoTabela` `#B30D1B47` | Cartões (tabelas e formulários) |
| `Dourado` `#E8C25A` | Títulos, ícones, bordas e botão principal |
| `DouradoSuave` `#80E8C25A` | Borda dos cartões |
| `TextoClaro` `#F2F4FA` | Texto comum |
| `TextoErro` `#FF9E9E` | Mensagens de validação e valores negativos |

Ícones do menu: SVG 24×24 em `Resources/Images/<entidade>_icon.svg`, traço `#E8C25A`.
