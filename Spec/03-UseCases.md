# Casos de uso

Ficam em `ComoEstamos.Core/UseCases/`, um arquivo por caso de uso.

## Quando criar

Toda operação chamada por uma tela tem um caso de uso, inclusive o CRUD simples
(decisão de 07/10/2026). O CRUD de cada entidade tem 5 casos de uso padrão, descritos em
[07-Crud.md](07-Crud.md), em `UseCases/<Entidades>/`. Os casos de uso abaixo são os que vão além do CRUD.

## Convenções

- Nome: `<Verbo><Objeto>UseCase`, com a interface `I<Verbo><Objeto>UseCase` no mesmo arquivo.
- Um único método público: `ExecutarAsync(...)`.
- Recebe `IUnitOfWorkFactory` no construtor e usa **uma** unidade de trabalho por execução
  (ver [02-UnitOfWork.md](02-UnitOfWork.md)).
- Ordem: **validar tudo → alterar → `ConfirmarAsync()`**. Nenhuma alteração antes da última validação.
- Consultas (buscar) também abrem a unidade de trabalho, mas não chamam `ConfirmarAsync`.
- Erros:
  - `RegraDeNegocioException`: operação recusada por regra. A mensagem é mostrada ao usuário.
  - `ValidacaoException` (herda da anterior): campos fora do domínio, com a lista de erros por campo.
  - `KeyNotFoundException`: o Id principal recebido não existe (mesmo padrão dos repositórios).
- Registro em `ComoEstamos.Core/ServiceCollectionExtensions.cs` (`AddComoEstamosCore`), como singleton.
- Testes em `ComoEstamos.Test/UseCaseTests/<Nome>Tests.cs` (ver [04-Testes.md](04-Testes.md)).

---

## Catálogo — implementados

### CriarDividaUseCase

Cadastra uma dívida ou crédito e gera as parcelas. Fica em `UseCases/Dividas/` e é chamado por
`IDividaService.IncluirAsync` (tela Incluir Dívida). Assinatura: `ExecutarAsync(Divida divida, int idUsuario)`.
A validação dos campos fica em `DividaValidador` e o cálculo das parcelas (valor, vencimento, descrição) em
`Parcelamento`, ambos em `UseCases/Dividas/` e usados também pelo `AlterarDividaUseCase`.

**Repositórios:** Usuarios, Contas, Categorias, Credores (leitura) · Dividas, Parcelas (escrita)

**Dono:** a dívida **não tem `id_usuario`** (saiu de `tb_divida` na migration `RemoverUsuarioDivida`, decisão de
16/10/2026): o dono é o **da conta dela** (`Divida.Conta.IdUsuario`). Por isso a conta é obrigatória e precisa ser do
usuário recebido (validação abaixo). Consultas e checagens de dono usam
`DividaValidador.EhDoUsuarioAsync` (casos de uso) ou `Divida.Conta.IdUsuario` (repositórios). Zera o `Id` e grava
ativa. Usuário inexistente ou desativado → `RegraDeNegocioException`.

**Validações** (todas juntas numa `ValidacaoException`, um erro por campo; nada gravado):

| Campo | Regra |
|---|---|
| `Nome` | Obrigatório, sem espaços nas pontas, até 100 caracteres. Pode repetir (não é único) |
| `Valor` | Valor total: de 0,01 a 99.999.999,99, até 2 casas |
| `NumeroParcelas` | De 1 a 360 (`DividaValidador.MaximoParcelas`) |
| `DiaVencimento` | De 1 a 31 |
| `IdConta`, `IdCategoria` | Obrigatórias, ativas e do mesmo usuário |
| `IdCredor` | Opcional; se informado, ativo e do mesmo usuário |

**Geração das parcelas:**

- `Divida.Valor` é o **valor total**. Cada parcela recebe `Valor / NumeroParcelas`, truncado
  em 2 casas; **os centavos que sobrarem vão para a última parcela**
  (R$ 100 em 3x → 33,33 · 33,33 · 33,34).
- 1ª parcela: vence em `DataPrimeiroVencimento` (só a data, sem hora).
- Demais: nos meses seguintes, no dia `DiaVencimento`. Se o mês não tem esse dia, vence no
  último dia do mês (dia 31 → 28/02, 31/03, 30/04...).
- Descrição: `"<Nome> <n>/<total>"` (ex.: `Notebook 3/12`). Se passar de 100 caracteres, o nome é cortado.
- Conta das parcelas = a da dívida. A parcela não tem categoria própria: a dela é sempre a da dívida
  (`tb_divida.id_categoria`; a coluna `id_categoria` saiu de `tb_parcela` na migration `RemoverCategoriaParcela`).
  As parcelas nascem ativas e sem pagamento.
- Parcelas que vierem preenchidas em `divida.Parcelas` são descartadas; quem gera é o caso de uso.
- Crédito (`EhDivida = false`) gera parcelas da mesma forma.

### AlterarDividaUseCase

Decisão de 13/10/2026 (opção "refazer só as em aberto"). Chamado por `IDividaService.AlterarAsync`.
Assinatura: `ExecutarAsync(Divida divida, int idUsuario)`.

**Repositórios:** Contas, Categorias, Credores (leitura) · Dividas, Parcelas (escrita)

- Inexistente ou de outro usuário → `KeyNotFoundException`; excluída → `RegraDeNegocioException`.
- Valida os campos como a inclusão (`DividaValidador`). `EhDivida` não muda: vale o gravado, mesmo que a edição
  traga outro valor.
- Mensagens conforme o tipo: "o nome da receita", "parcelas recebidas", "já foram recebidos" e "O pagador escolhido"
  na receita (`DividaValidador.Tipo`/`Pagas`). O `PagarParcelaUseCase` também: "A data do recebimento não pode ser futura."
- **As parcelas pagas nunca mudam** de valor, vencimento ou conta (só a descrição, abaixo).

**Campos simples** (nome, credor, conta, categoria), sem mudar nenhum campo financeiro: as parcelas continuam
as mesmas. A descrição de todas é refeita com o novo nome (`<Nome> <n>/<total>`, na ordem de vencimento); a
nova conta vale **só para as em aberto**. A nova categoria vale para todas, porque fica só na dívida.

**Campos financeiros** (valor total, número de parcelas, dia ou primeiro vencimento) mudaram:

| Regra (só quando há parcelas pagas) | Campo do erro |
|---|---|
| O primeiro vencimento não pode mudar | `DataPrimeiroVencimento` |
| Número de parcelas ≥ quantidade de pagas | `NumeroParcelas` |
| Sobrando parcelas em aberto, o total deve cobrir o já pago mais R$ 0,01 por parcela em aberto | `Valor` |
| Todas pagas (número de parcelas = pagas): o total deve ser igual ao já pago | `Valor` |

- As parcelas **em aberto são desativadas** (não apagadas) e são geradas novas, de `k+1` até o total (`k` =
  pagas), dividindo **o que falta pagar** (total − pago) igualmente, com os centavos na última.
- Os vencimentos das novas seguem a regra da inclusão pela posição (a parcela `n` vence no mês `n−1` a partir do
  primeiro vencimento, no `DiaVencimento`). As pagas ficam com os números `1..k`, por ordem de vencimento.
- Ex.: 12x de R$ 100 com 2 pagas, alterada para R$ 1.000 em 10x → as 2 pagas ficam; 8 novas de R$ 100 (3/10 a 10/10).

### ExcluirDividaUseCase

Decisão de 13/10/2026 (opção "excluir tudo sem mexer no saldo"). Chamado por `IDividaService.ExcluirAsync`.

- Inexistente ou de outro usuário → `KeyNotFoundException`; já excluída → `RegraDeNegocioException`.
- Desativa a dívida e **todas** as parcelas ativas dela, pagas ou não. Nada é apagado do banco.
- **O saldo das contas não muda.**
- Depois disso, a dívida e as parcelas não impedem mais a exclusão da conta, da categoria ou do credor.

### BuscarDividasUseCase

Consulta da tela de dívidas. Fica em `UseCases/Dividas/` e é chamado por `IDividaService.BuscarAsync`.
Assinatura: `ExecutarAsync(FiltroDividas filtro, int idUsuario)` → `List<ResumoDivida>`.

**Repositórios:** Dividas, Parcelas (leitura; uma consulta de cada, agrupadas em memória)

- Só **dívidas** (`EhDivida = true`) ativas do usuário, ou só **receitas** com `FiltroDividas.EhDivida = false`
  (tela de Receitas). Nunca as duas juntas.
- Considera só as parcelas **ativas**. A dívida está **paga** quando tem parcelas e todas têm `DataPagamento`
  (não há coluna de situação na dívida).
- `ResumoDivida`: nome, dia de vencimento, número de parcelas, valor total, valor da parcela
  (o da próxima parcela em aberto; sem parcelas, total ÷ parcelas truncado), parcelas pagas, próximo vencimento
  (1ª parcela em aberto) e se está paga.

| Filtro (`FiltroDividas`) | Regra |
|---|---|
| `Nome` | Parte do nome, sem diferenciar maiúsculas nem acentos ("cartao" acha "Cartão"); espaços nas pontas ignorados; vazio não filtra |
| `MesVencimento` | Dívidas com **alguma parcela** vencendo no mês/ano (o dia é ignorado); nulo não filtra |
| `OcultarPagas` | Remove as pagas |
| `EhDivida` | `true` (padrão): dívidas; `false`: receitas |

**Ordem:** as em aberto pelo **próximo vencimento**; depois as pagas, pelo último vencimento. Empate: por nome.

### BuscarResumoMesUseCase

Decisão de 14/10/2026. Consulta da tela Resumo do Mês ([05-Telas.md](05-Telas.md#resumo-do-mês)). Fica em
`UseCases/Dividas/` e é chamado por `IDividaService.BuscarResumoMesAsync`. Assinatura:
`ExecutarAsync(DateTime mes, int idUsuario)` → `ResumoMes` (o dia de `mes` é ignorado).

**Repositórios:** Dividas, Parcelas (leitura; uma consulta de cada, agrupadas em memória)

- Dívidas e créditos **ativos** do usuário com alguma **parcela ativa** vencendo no mês. Sem parcela no mês, não
  aparecem.
- Um `ItemResumoMes` por dívida: nome, **soma das parcelas do mês** (em geral uma só), vencimento da primeira delas,
  `Paga` (todas as do mês pagas) e a data do último pagamento delas.
- `Despesas` = `EhDivida = true`; `Receitas` = `EhDivida = false`. Ordem: pelo vencimento; empate, por nome.
- Totais: `TotalDespesas`, `TotalReceitas` e `Diferenca` = receitas − despesas (pagas e em aberto somam igual).

### BuscarDividaPorIdUseCase

Dívida da tela Visualizar, com `Credor`, `Conta`, `Categoria` e as **parcelas ativas** por vencimento.
`null` se não existe, é de outro usuário ou está inativa (mesma regra do BuscarPorId do CRUD).

### Parcelas (`UseCases/Parcelas/`)

Decisão de 14/10/2026. Chamados pelo `IParcelaService` (tela Visualizar Parcela, ver [05-Telas.md](05-Telas.md#parcelas)).
A parcela não tem `id_usuario`: o dono é conferido **pela conta da dívida** (`Divida.Conta.IdUsuario`). Regras comuns em
`ParcelaValidador`:

- Inexistente ou de outro usuário → `KeyNotFoundException`. Parcela ou dívida excluída → `RegraDeNegocioException`.
- **Parcela paga não muda**: não pode ser alterada nem excluída.
- Depois de incluir, alterar ou excluir, a dívida é **recalculada** a partir das parcelas ativas: valor total = soma delas,
  número de parcelas = quantidade, primeiro vencimento = o da primeira, e as descrições refeitas na ordem de
  vencimento (`<Nome> <n>/<total>`; no empate de vencimento, a recém-incluída fica por último). O dia de vencimento
não muda.

| Caso de uso | Assinatura | Regras |
|---|---|---|
| `BuscarParcelaPorIdUseCase` | `ExecutarAsync(id, idUsuario)` → `Parcela?` | Com `Conta` e `Divida` (com a `Categoria` e as parcelas ativas dela, por vencimento, para a tela saber o número). `null` se não existe, é de outro usuário ou se a parcela ou a dívida foi excluída |
| `IncluirParcelaUseCase` | `ExecutarAsync(Parcela, idUsuario)` → `Parcela` | Na dívida `IdDivida` (ativa e do usuário: senão `KeyNotFoundException`/`RegraDeNegocioException`). Grava só valor, vencimento (só a data) e conta: nasce ativa e **em aberto**. Recusa passar de 360 parcelas. Recalcula a dívida. O saldo não muda |
| `AlterarParcelaUseCase` | `ExecutarAsync(Parcela, idUsuario)` | Só parcela em aberto. Grava valor, vencimento (só a data) e conta e recalcula a dívida |
| `ExcluirParcelaUseCase` | `ExecutarAsync(id, idUsuario)` | Só parcela em aberto; desativa (não apaga) e recalcula a dívida. **A única parcela ativa não pode ser excluída** (exclua a dívida). O saldo não muda |
| `PagarParcelaUseCase` | `ExecutarAsync(id, dataPagamento, idUsuario)` | Ver abaixo |

**Validação do `IncluirParcelaUseCase` e do `AlterarParcelaUseCase`** (`ParcelaValidador.ValidarCamposAsync`) (`ValidacaoException`, um erro por campo; nada gravado):

| Campo | Regra |
|---|---|
| `Valor` | De 0,01 a 99.999.999,99, até 2 casas. O novo total da dívida (soma das parcelas) também não pode passar de 99.999.999,99 |
| `IdConta` | Obrigatória, ativa e do mesmo usuário |
| `DataVencimento` | Livre. Pode mudar a ordem das parcelas: a numeração das descrições acompanha |

Se depois a **dívida** for alterada num campo financeiro, o `AlterarDividaUseCase` refaz as parcelas em aberto
pela regra da inclusão, e os ajustes feitos à mão nelas se perdem.

#### PagarParcelaUseCase

Registra o pagamento (ou recebimento) de uma parcela.

**Repositórios:** Parcelas, Dividas

**Validações:** parcela do usuário (pela dívida), ativa e ainda não paga; a dívida deve estar ativa;
a data do pagamento não pode ser futura (`ValidacaoException`, campo `DataPagamento`).

**Efeito:** `Parcela.DataPagamento = dataPagamento` (só a data).

**O saldo da conta não muda** (decisão de 14/10/2026; até então, a dívida debitava e a receita creditava a conta).
O saldo é atualizado pelo usuário em "Atualizar Saldos". Por isso o pagamento também não grava `tb_historico_saldo`.

### Operações (`UseCases/Operacoes/`)

Decisão de 20/10/2026. Chamados pelo `IOperacaoService` (tela Visualizar Investimento e Incluir Operação, ver
[05-Telas.md](05-Telas.md#operações)). A operação não tem `id_usuario`: o dono é conferido **pela carteira do
investimento** (`InvestimentoValidador.ObterDoUsuarioAsync`). **Não há alteração** de operação: para corrigir, exclui e
registra de novo. Testes em `UseCaseTests/OperacaoUseCaseTests.cs`.

#### RegistrarOperacaoUseCase

Registra a compra ou venda de um investimento e atualiza a quantidade em carteira.
Assinatura: `ExecutarAsync(Operacao operacao, int idUsuario)`.

**Repositórios:** Carteiras (leitura) · Investimentos, Operacoes (escrita)

**Validações:** o investimento deve existir e ser do usuário (`KeyNotFoundException`) e estar ativo
(`RegraDeNegocioException`). Por campo (`ValidacaoException`): `Quantidade` com até 6 casas, de 0,000001 a
999.999.999.999,999999; `ValorTotal` com até 2 casas, de 0 a 99.999.999,99; `DataOperacao` (data **e hora**) não pode
ser futura (comparada com `DateTime.Now`); na venda, `Quantidade` ≤ quantidade disponível; na compra, a quantidade
resultante não pode passar do limite.

**Efeito:** compra → `Investimento.Quantidade += Quantidade`; venda → `-=`. Grava uma operação nova só com os campos
do formulário (nasce ativa). A cotação do investimento **não** é alterada.

`Operacao.ValorTotal` é o **valor total** da operação (quantidade × preço unitário), não o
preço unitário. Quando for preciso o preço unitário (preço médio, rentabilidade), calcule
`ValorTotal / Quantidade`.

#### ExcluirOperacaoUseCase

Desativa a operação e **desfaz o efeito** dela na quantidade do investimento: compra excluída → `-= Quantidade`; venda
excluída → `+= Quantidade`. Inexistente ou de outro usuário → `KeyNotFoundException`; operação já excluída ou investimento
excluído → `RegraDeNegocioException`. Recusa (`RegraDeNegocioException`) se a quantidade ficasse negativa (ex.: compra
de 50 seguida de venda de 120, numa posição de 100) ou passasse do limite.

#### BuscarUltimasOperacoesUseCase

`ExecutarAsync(int idInvestimento, int quantidade, int idUsuario)`: as `quantidade` operações **ativas** mais recentes,
por `DataOperacao` decrescente (no empate, a de maior Id primeiro), pelo
`IOperacaoRepository.ListarUltimasPorInvestimentoAsync`. Lista vazia se o investimento não existe, é de outro usuário
ou está inativo.

### AtualizarSaldosUseCase

Atualiza de uma vez os saldos de várias contas (tela "Atualizar Saldos", ver [05-Telas.md](05-Telas.md)).
Fica em `UseCases/Contas/` e é chamado por `IContaService.AtualizarSaldosAsync`.

**Repositórios:** Contas, HistoricosSaldo

**Entrada:** lista de `NovoSaldo(IdConta, Saldo)`. Conta repetida → `ArgumentException`.

**Validações** (antes de gravar qualquer coisa):

- Cada conta deve existir e ser do usuário (`KeyNotFoundException`) e estar ativa (`RegraDeNegocioException`).
- Cada saldo segue a regra de `nr_saldo` (2 casas, ±99.999.999,99). Os erros vêm juntos numa
  `ValidacaoException`, com o campo `Saldo:<idConta>` (`AtualizarSaldosUseCase.CampoSaldo`).

**Efeito:** só as contas cujo saldo mudou são gravadas; para cada uma, uma linha em
`tb_historico_saldo` com o novo saldo, todas com a mesma data/hora. Devolve quantas contas mudaram.

### Histórico de saldo (`tb_historico_saldo`)

Decisão de 13/10/2026: toda mudança de saldo feita pelo usuário fica registrada em `tb_historico_saldo`
(`HistoricoSaldo`: data, conta, saldo, mais as colunas de `EntidadeBase`).

| Quem grava | Quando |
|---|---|
| `AtualizarSaldosUseCase` | Para cada conta cujo saldo mudou |
| `AlterarContaUseCase` (CRUD de Conta) | Se o saldo mudou |

- A inclusão de conta **não** grava histórico.
- `PagarParcelaUseCase` **não** grava histórico: ele não mexe no saldo.
- O histórico não impede excluir a conta. Decisão de 14/10/2026: o `ExcluirContaUseCase` desativa junto
  (`dm_ativo = false`, na mesma unidade de trabalho) todos os históricos ativos da conta
  (`HistoricoSaldoRepository.DesativarPorContaAsync`). Exclusão recusada não mexe no histórico.

### Backup e restore (`UseCases/Backup/`)

Decisão e implementação de 14/10/2026. Chamados pelo `IBackupService` (tela Configurações → Backup, ver
[05-Telas.md](05-Telas.md#configurações)). Só **manual**: não há backup automático local nem agendado.

**Arquivo de backup:** um `.zip` (não uma extensão própria: o seletor de arquivos do Android não a reconhece
bem), com o nome `como-estamos-backup-AAAA-MM-DD-HHmm.zip`, contendo:

| Entrada | Conteúdo |
|---|---|
| `comoestamos.db3` | Cópia **consistente** do banco SQLite |
| `manifest.json` | `formato` (versão do formato do backup, começa em 1), `versaoApp`, `ultimaMigration` (Id da última migration aplicada), `dataHora` e `contagens` (registros **ativos** por tabela: contas, dívidas, receitas, parcelas, categorias, credores, carteiras, investimentos) |

- O arquivo vai **sem senha** por enquanto (proteção com senha está em [06-Pendencias.md](06-Pendencias.md)).
- O backup leva o banco inteiro (todos os usuários e os registros inativos). Hoje só há o usuário local.

| Caso de uso | Assinatura | Regras |
|---|---|---|
| `GerarBackupUseCase` | `ExecutarAsync(string pastaDestino, string versaoApp)` → caminho do `.zip` | Copia o banco com `VACUUM INTO` (ou `SqliteConnection.BackupDatabase`), **nunca** copiando o arquivo `.db3` direto: o SQLite pode ter gravações pendentes no `-wal` e a cópia sairia incompleta. Monta o `manifest.json` e compacta. Os arquivos temporários são apagados |
| `ValidarBackupUseCase` | `ExecutarAsync(string caminhoZip)` → `ResumoBackup` | Não muda nada. Recusa (`RegraDeNegocioException`, mensagem para o usuário) se: não é `.zip` válido; falta o `manifest.json` ou o `comoestamos.db3`; `formato` desconhecido; `PRAGMA integrity_check` diferente de `ok`; `ultimaMigration` **não existe nesta versão do app** (backup de versão mais nova: "Atualize o app para restaurar este backup"). Devolve `ResumoBackup`: data/hora, versão do app e contagens **do manifesto** (contar no banco do backup dependeria do esquema daquela versão) |
| `RestaurarBackupUseCase` | `ExecutarAsync(string caminhoZip, string versaoApp)` | Valida de novo (como o `ValidarBackupUseCase`). Antes de trocar, grava um **backup de segurança** do banco atual na pasta do app (`backup-antes-do-restore.zip`, só o último, substituído a cada restore). Fecha as conexões (`SqliteConnection.ClearAllPools()`), **substitui o banco inteiro** (não mistura com os dados atuais) e roda as migrations (`Database.Migrate()`), o que atualiza um backup de versão anterior. Apaga o `-wal`/`-shm` do banco antigo (seriam aplicados ao novo). Se a troca ou a migration falhar, volta o banco de antes e lança o erro |

Os casos de uso de backup **não** usam a unidade de trabalho: recebem o `IArquivoBanco` (`ComoEstamos.Data/ArquivoBanco.cs`),
que faz a cópia (`VACUUM INTO`), o `integrity_check`, lê as migrations aplicadas/conhecidas, conta os registros ativos
(`ContagemRegistros`) e troca o arquivo. O formato do zip e a validação comum ficam em `PacoteBackup` (interno do Core):
só as duas entradas conhecidas são lidas, pelo nome (nada é extraído com o caminho que vem no zip). Os arquivos
avulsos (cópias de backup) são abertos **sem pool** de conexões, para poderem ser apagados logo depois.

**Onde fica cada parte** ([01-Arquitetura.md](01-Arquitetura.md)): `IBackupService` e os casos de uso no Core; o que é
específico do SQLite no ComoEstamos.Data (`IArquivoBanco`, registrado por `AddComoEstamosData` com o caminho do
banco); compartilhar (`Share.Default`), escolher o arquivo (`FilePicker`) e a versão do app (`AppInfo`) na página.

**Testes** (`UseCaseTests/BackupUseCaseTests.cs`, ver [04-Testes.md](04-Testes.md)): com SQLite em **arquivo
temporário** (não InMemory), gerar (zip, manifesto, contagens só de ativos), validar (resumo; recusa não-zip, zip sem
manifesto, `formato` desconhecido, banco corrompido e migration desconhecida) e restaurar (substitui tudo e grava o
backup de segurança; recusado não muda nada; backup de migration anterior é migrado). A volta do banco quando a troca
falha não tem teste (precisaria simular a falha).

---

## Catálogo — candidatos futuros

Operações já identificadas que envolvem mais de um repositório. Especifique aqui antes de implementar.

| Caso de uso | Envolve | Decisões em aberto |
|---|---|---|
| `EstornarPagamentoParcelaUseCase` | Parcelas, Dividas | Precisa de um método no repositório para limpar `DataPagamento`; o saldo não muda (como no `PagarParcela`) |
| `TransferirEntreContasUseCase` | Contas | Uma só tabela, mas duas contas precisam mudar juntas |
| `RegistrarHistoricoUseCase` | Investimentos, Historicos | Retrato da carteira inteira ou de um investimento; periodicidade (manual ou mensal) |
