# Telas (MAUI)

## Organização

| Onde | O quê |
|---|---|
| `MainPage.xaml` | Tela inicial: título e tabela de contas com saldo |
| `Pages/` | Demais telas. Uma página por arquivo (`ContasPage`, `ContaPage`...) |
| `Controls/` | Componentes reaproveitados entre telas (`TabelaContas`) |
| `Services/` | Serviços do app que não são regra de negócio (`UsuarioAtual`) |
| `Resources/Styles/Colors.xaml` | Paleta do app (ver abaixo) |

- As telas usam **code-behind**, sem MVVM nem CommunityToolkit. A página recebe os
  repositórios, casos de uso e serviços pelo construtor.
- Toda página é registrada como `AddTransient` em `MauiProgram`.
- Quem a tela chama segue [01-Arquitetura.md](01-Arquitetura.md): o repositório para operações
  de uma entidade e um caso de uso para operações com mais de um repositório ou com regra de negócio.

## Usuário atual

Ainda não há login. `IUsuarioAtual.ObterIdAsync()` devolve o **usuário local padrão**
(`local@comoestamos.app`) e o cria na primeira chamada se ele não existir. Toda tela que lista
ou grava dados de um usuário usa esse Id (`ListarPorUsuarioAsync(await usuarioAtual.ObterIdAsync())`).
Quando houver login, só o `UsuarioAtual` muda.

## Navegação

- `AppShell` tem um **menu lateral (flyout)** com Início (`MainPage`) e Contas (`ContasPage`).
- As páginas escondem a barra de navegação (`Shell.NavBarIsVisible="False"`). As páginas do menu
  têm o ícone ☰ dourado no canto superior esquerdo, que abre o menu (`Shell.Current.FlyoutIsPresented = true`).
- Páginas fora do menu são registradas com `Routing.RegisterRoute` em `AppShell.xaml.cs`, e a página
  expõe a rota numa constante (`ContaPage.Rota`). Parâmetros vão na query (`conta?id=3`) e são lidos com
  `IQueryAttributable`. Para voltar, use `Shell.Current.GoToAsync("..")`.

## Padrão de CRUD

Exemplo: Contas (`ContasPage` + `ContaPage`).

- **Lista**: mostra só registros ativos e recarrega em `OnAppearing`. Tocar na linha abre a alteração.
  O botão **+** (sinal dourado sobre um círculo, no canto superior direito) abre a inclusão.
- **Inclusão e alteração** ficam na **mesma página**. Sem `id` ela inclui; com `id` ela carrega o
  registro, troca o título para "Alterar ..." e mostra a lixeira.
- Os títulos dos campos ficam em português correto (`nm_conta` → "Nome da Conta"). Os botões
  **Cancelar** e **Salvar** ficam no fim do formulário e os dois voltam para a lista.
- **Validação** antes de gravar: cada campo errado mostra a mensagem logo abaixo dele, e nada é gravado.
- **Exclusão**: a lixeira (canto superior direito, sobre um círculo) pede confirmação com
  `DisplayAlertAsync(..., "Sim", "Não")`. "Sim" chama `DesativarAsync` (exclusão lógica) e volta para a lista.
  "Não" fica na tela.
- Valores em dinheiro são exibidos com `ToString("C", pt-BR)`. Na digitação, aceitam o formato
  brasileiro (`1.250,50`) e o ponto decimal de teclados numéricos (`1250.5`).

### Contas: regras do formulário

| Campo | Título | Regra |
|---|---|---|
| `nm_conta` | Nome da Conta | Obrigatório, até 100 caracteres (espaços nas pontas são removidos) |
| `nr_saldo` | Saldo (R$) | Obrigatório, até 2 casas decimais, entre -99.999.999,99 e 99.999.999,99; negativo é permitido |

A imagem (`img_conta`) ainda não aparece no formulário.

## Visual

Todas as telas usam `fundo.png` (`Aspect="AspectFill"`) e `Padding="20,50,20,20"`, com título
dourado de 32 pt e uma linha dourada de 2 px abaixo dele.

| Cor (`Colors.xaml`) | Uso |
|---|---|
| `AzulFundo` `#1A2E6E` | Fundo da página atrás da imagem |
| `AzulEscuro` `#0A1438` | Cabeçalho de tabela, campos, círculo dos botões de ícone, menu lateral |
| `FundoTabela` `#B30D1B47` | Cartões (tabelas e formulários) |
| `Dourado` `#E8C25A` | Títulos, ícones, bordas e botão principal |
| `DouradoSuave` `#80E8C25A` | Borda dos cartões |
| `TextoClaro` `#F2F4FA` | Texto comum |
| `TextoErro` `#FF9E9E` | Mensagens de validação e saldo negativo |

Botão principal: fundo `Dourado` e texto `AzulEscuro`. Botão secundário: transparente, com borda e texto `Dourado`.
