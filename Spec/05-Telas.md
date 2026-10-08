# Telas (MAUI)

## Organização

| Onde | O quê |
|---|---|
| `MainPage.xaml` | Tela inicial: título e tabela de contas com saldo |
| `Pages/<Entidades>/` | As 4 páginas do CRUD de cada entidade (`ListarContasPage`, `VisualizarContaPage`, `IncluirContaPage`, `AlterarContaPage`). Ver [07-Crud.md](07-Crud.md) |
| `Pages/Formulario.cs` | Passos repetidos das páginas: ler o Id da rota, ler números, mostrar erros nos campos, salvar, excluir, voltar |
| `Controls/TabelaRegistros` | Tabela das listas: coluna de texto (com detalhe opcional) e coluna de valor opcional |
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

- `AppShell` tem um **menu lateral (flyout)**: Início, Contas, Categorias, Credores, Carteiras e Investimentos.
  Cada item de CRUD abre a página Listar (rota no plural: `contas`, `carteiras`...).
- As páginas escondem a barra de navegação (`Shell.NavBarIsVisible="False"`). As páginas do menu
  têm o ícone ☰ dourado no canto superior esquerdo, que abre o menu (`Shell.Current.FlyoutIsPresented = true`).
- Páginas fora do menu são registradas com `Routing.RegisterRoute` em `AppShell.xaml.cs`, e a página
  expõe a rota numa constante (`VisualizarContaPage.Rota = "visualizar-conta"`). Parâmetros vão na
  query (`alterar-conta?id=3`) e são lidos com `IQueryAttributable` + `Formulario.LerId`. Para voltar,
  `Formulario.VoltarAsync()` (`GoToAsync("..")`).

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

**Conta** — exclusão impedida se houver dívidas ou parcelas ativas na conta.

| Campo | Título | Regra |
|---|---|---|
| `nm_conta` | Nome da Conta | Até 100 caracteres |
| `nr_saldo` | Saldo (R$) | Obrigatório, até 2 casas, entre -99.999.999,99 e 99.999.999,99; negativo é permitido |

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
| `Campo` (Entry), `CampoTextoLongo` (Editor), `CampoEscolha` (Picker) | Controles de entrada |
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
