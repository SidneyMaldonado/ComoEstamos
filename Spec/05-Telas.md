# Telas (MAUI)

## Organização

| Onde | O quê |
|---|---|
| `MainPage.xaml` | Tela inicial: título, tabela de contas com saldo e total no rodapé, botão "Atualizar Saldos" e o atalho para as dívidas (abaixo) |
| `Compartilhamento/ImagemSaldos.cs` | PNG da tabela de saldos compartilhada pela tela inicial (SkiaSharp) |
| `Pages/<Entidades>/` | As 4 páginas do CRUD de cada entidade (`ListarContasPage`, `VisualizarContaPage`, `IncluirContaPage`, `AlterarContaPage`). Ver [07-Crud.md](07-Crud.md) |
| `Pages/Formulario.cs` | Passos repetidos das páginas: ler o Id da rota, ler números, mostrar erros nos campos, salvar, excluir, voltar |
| `Controls/TabelaRegistros` | Tabela das listas: coluna de texto (com detalhe opcional), coluna de valor opcional e rodapé de total opcional (`Total`; nulo esconde). `RolaSozinha` (padrão `true`): as linhas rolam dentro da tabela; `false`: a tabela cresce com as linhas (pilha com `BindableLayout`), para ficar numa `ScrollView` da página. `Montar(..., apagado:)` deixa o valor da linha num tom mais fraco |
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

- `AppShell` tem um **menu lateral (flyout)**: Início, Resumo do Mês, Contas, Dívidas, Receitas, Categorias, Credores, Carteiras e Investimentos.
  Cada item de CRUD abre a página Listar (rota no plural: `contas`, `carteiras`...).
- As páginas escondem a barra de navegação (`Shell.NavBarIsVisible="False"`). As páginas do menu
  têm o ícone ☰ dourado no canto superior esquerdo, que abre o menu (`Shell.Current.FlyoutIsPresented = true`).
- Páginas fora do menu são registradas com `Routing.RegisterRoute` em `AppShell.xaml.cs`, e a página
  expõe a rota numa constante (`VisualizarContaPage.Rota = "visualizar-conta"`). Parâmetros vão na
  query (`alterar-conta?id=3`) e são lidos com `IQueryAttributable` + `Formulario.LerId`. Para voltar,
  `Formulario.VoltarAsync()` (`GoToAsync("..")`).

- **Atalho da tela inicial para o resumo do mês** (até 14/10/2026 abria as dívidas): arrastar o dedo para cima na
  MainPage (`SwipeGestureRecognizer` `Up` no conteúdo) abre `//resumo`, como o item do menu. As linhas da tabela de
  saldos continuam rolando: o gesto vale fora delas (título, espaço abaixo da tabela, botão). A dica "︿ Arraste
  para cima para ver o resumo do mês", abaixo do botão "Atualizar Saldos", também abre o resumo ao ser tocada (no
  Windows, o arrasto com o mouse não dispara o gesto).
- **Do resumo para as dívidas**: arrastar o dedo para cima na `ResumoMesPage` abre `//dividas`. A rolagem das
  tabelas consome o arrasto, então o gesto vale no título e na linha do mês; no fim da rolagem, a dica "︿ Arraste
  para cima para ver as dívidas" também abre as dívidas ao ser tocada.
- **Volta, na mesma ordem** (Dívidas ↓ Resumo ↓ Início): arrastar o dedo para baixo na `ListarDividasPage` abre
  `//resumo` (vale fora das linhas da tabela e do campo de busca); na `ResumoMesPage`, abre `//MainPage` (no título e
  na linha do mês). Sem dica na tela.
- **Das dívidas para as receitas** (14/10/2026): arrastar o dedo para cima na `ListarDividasPage` abre `//receitas`;
  na `ListarReceitasPage`, arrastar para baixo volta para `//dividas`. Valem fora das linhas da tabela e do campo de
  busca. Sem dica na tela (no Windows, as receitas ficam no menu). Ordem completa: Início ↑ Resumo ↑ Dívidas ↑ Receitas.
- **Da tela inicial para os investimentos**: arrastar o dedo para baixo na MainPage abre `//investimentos`; na
  `ListarInvestimentosPage`, arrastar para cima volta para `//MainPage`. Valem fora das linhas da tabela. Sem dica
  na tela (no Windows, os investimentos ficam no menu).

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
`Pages/Dividas/` e usam o `IDividaService`. **Só dívidas** (`EhDivida = true`): as receitas têm tela própria
([Receitas](#receitas)). A Visualizar e a Alterar Dívida tratam uma receita como "Dívida não encontrada" (e
vice-versa nas páginas de receita). Listar (consulta), Visualizar, Incluir e Alterar; excluir é a lixeira da Visualizar. Regras de alterar e excluir em
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
(`Paga em 05/10/2026`, `Vencida` ou `Em aberto`) e o valor; ela rola sozinha. **Tocar numa parcela** abre a
Visualizar Parcela ([Parcelas](#parcelas)). À direita do título "Parcelas", um **+** (círculo de 34 px, menor que
o do topo) abre a Incluir Parcela. Botão Voltar.
Parcelas: o valor mostrado é o da próxima parcela em aberto.

- **Lápis**: abre a Alterar.
- **Lixeira**: "Confirma a exclusão da dívida "X" e de todas as parcelas?" (Sim/Não). Se houver parcela paga,
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

## Receitas

Decisão de 14/10/2026. Receita = registro de `tb_divida` com `EhDivida = false` (crédito, valor a receber). As páginas
ficam em `Pages/Receitas/` (`ListarReceitasPage`, `VisualizarReceitaPage`, `IncluirReceitaPage`, `AlterarReceitaPage`),
**cópias das de Dívidas** com o mesmo layout e o mesmo `IDividaService` e casos de uso. Diferenças:

| Onde | Dívida | Receita |
|---|---|---|
| Rotas | `dividas`, `visualizar-divida`, `incluir-divida`, `alterar-divida` | `receitas`, `visualizar-receita`, `incluir-receita`, `alterar-receita` |
| Menu | "Dívidas" (`divida_icon`) | "Receitas" (`receita_icon`, logo abaixo de Dívidas) |
| Consulta | `FiltroDividas(..., EhDivida: true)` | `FiltroDividas(..., EhDivida: false)` |
| `id_credor` | Título "Credor" | Título **"Pagador"** (opcional), com a mesma lista de Credores |
| Situação | "Paga", "3 de 12 pagas", filtro "Ocultar dívidas pagas" | "Recebida", "3 de 12 recebidas", "Ocultar receitas recebidas" |
| Exclusão com parcela paga | "Os pagamentos já feitos continuam descontados do saldo da conta." | "Os recebimentos já feitos continuam somados ao saldo da conta." |
| Aviso da Alterar | "...parcelas já pagas... com o que falta pagar" | "...parcelas já recebidas... com o que falta receber" |

`dm_divida` é sempre `false` na Incluir Receita. As parcelas usam as mesmas páginas de [Parcelas](#parcelas).

## Parcelas

Páginas em `Pages/Parcelas/`, com o `IParcelaService`. Não há lista própria: a parcela é aberta pela tabela da
Visualizar Dívida (ou Receita), e incluída pelo **+** da seção Parcelas dela. Na parcela de **receita** os textos mudam
(`Pages/Parcelas/TextosParcela.cs`): título "Receita" no lugar de "Dívida", "Data do Recebimento", botão **Receber**,
"Recebida em 05/10/2026", confirmação "Receber parcela" / "Confirma o recebimento de ... O valor será somado ao saldo
da conta", e "A receita fica com uma parcela a menos..." na exclusão. Regras em [03-UseCases.md](03-UseCases.md#parcelas-usecasesparcelas).

**Visualizar** (`VisualizarParcelaPage`, rota `visualizar-parcela?id=`). Com rolagem. Cartão em duas colunas: Dívida
e Parcela (`2 de 12`); Valor e Vencimento; Situação (como na tabela da dívida) e Data do Pagamento (`—` em aberto);
Conta e Categoria; Descrição; Data de Cadastro e Última Alteração. Botão Voltar.

- **Parcela em aberto**: o cartão **Pagar** (Data do Pagamento, `DatePicker` que começa em hoje e não aceita data
  futura, e o botão Pagar). Pagar pede confirmação ("Confirma o pagamento de R$ 100,00 em 14/10/2026? O valor será
  descontado do saldo da conta \"Corrente\"." — no crédito, "somado ao"); Sim paga e a página recarrega, mostrando a
  parcela paga.
- **Lápis**: abre a Alterar Parcela.
- **Lixeira**: "Confirma a exclusão da parcela \"Notebook 3/12\" (R$ 100,00)? A dívida fica com uma parcela a menos e
  o valor total diminui." (Sim/Não). Sim exclui e volta para a Visualizar Dívida. A exclusão da única parcela é recusada
  com a mensagem num alerta.
- **Parcela paga**: lápis e lixeira apagados (opacidade 0,35) e sem efeito; sem o cartão Pagar.

**Incluir** (`IncluirParcelaPage`, rota `incluir-parcela?id=`, com o Id da **dívida**). O mesmo formulário da Alterar,
preenchido com o valor da última parcela, o vencimento do mês seguinte a ela (no dia de vencimento da dívida, ou no
último dia do mês) e a conta e a categoria da dívida. Acima, o aviso "Nova parcela de \"X\". Ela entra na ordem do
vencimento, e o valor total da dívida passa a ser a soma das parcelas.". Grava pelo `IncluirParcelaUseCase` e volta.

**Alterar** (`AlterarParcelaPage`, rota `alterar-parcela?id=`). Acima do formulário, a descrição da parcela e o
aviso "O valor total da dívida passa a ser a soma das parcelas.". Grava pelo `AlterarParcelaUseCase`. Se a parcela
estiver paga ao abrir, avisa e volta.

| Campo | Título | Regra |
|---|---|---|
| `nr_valor` | Valor (R$) | Mesmos campos na Incluir. Obrigatório, até 2 casas, de 0,01 a 99.999.999,99; o total da dívida também não pode passar disso |
| `dt_vencimento` | Vencimento | `DatePicker`; livre (pode mudar a ordem das parcelas) |
| `id_conta` | Conta | Obrigatória (`Picker` com as contas ativas) |
| `id_categoria` | Categoria | Obrigatória (`Picker` com as categorias ativas) |

## Resumo do Mês

`Pages/Resumo/ResumoMesPage`, rota `resumo` (item "Resumo do Mês" do menu, logo abaixo de Início, ícone
`resumo_icon.svg`, e o atalho da tela inicial). Versão no app da planilha "Como Estamos": o que vence no mês, as
receitas e a diferença. Usa o `IDividaService.BuscarResumoMesAsync` ([03-UseCases.md](03-UseCases.md#buscarresumomesusecase)).

- Atalhos: para cima abre as dívidas, para baixo volta para a tela inicial (ver "Navegação"); dica no fim da rolagem.
- Topo: ☰ e "Resumo do Mês". Abaixo, o mês: `‹ Outubro/2026 ›`. Começa no mês atual; as setas andam um mês; tocar no
  texto volta para o mês atual. O mês continua ao voltar para a página. Recarrega em `OnAppearing`.
- Uma `ScrollView` com três `TabelaRegistros` (`RolaSozinha="False"`), todas com o nome do mês no título da coluna
  de valor (como a planilha):
  - **Dívidas**: uma linha por dívida (`EhDivida = true`) com parcela vencendo no mês, com o nome e, abaixo,
    "Paga em 05/10", "Vencida em 10/10" ou "Vence em 10/10"; à direita, a soma das parcelas do mês. O valor das
    pagas fica num tom mais fraco. Rodapé "Total". Vazia: "Nenhuma dívida vence neste mês.";
  - **Receitas**: o mesmo para os créditos (`EhDivida = false`), com "Recebida em 05/10". Vazia: "Nenhuma receita neste mês.";
  - **Resumo**: linhas Receitas e Despesas e, no rodapé, a **Diferença** (receitas − despesas; vermelha quando
    negativa).
- Tocar numa linha de Dívidas abre a Visualizar Dívida; de Receitas, a Visualizar Receita (de onde se paga ou recebe
  a parcela). As linhas do Resumo não abrem nada.
- Dívidas sem parcela no mês não aparecem (na planilha aparecem com "-").

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
