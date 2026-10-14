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

**Dono:** como no CRUD ([07-Crud.md](07-Crud.md#dono-do-registro)), sobrescreve `IdUsuario` com o usuário
recebido, zera o `Id` e grava ativa. Usuário inexistente ou desativado → `RegraDeNegocioException`.

**Validações** (todas juntas numa `ValidacaoException`, um erro por campo; nada gravado):

| Campo | Regra |
|---|---|
| `Nome` | Obrigatório, sem espaços nas pontas, até 100 caracteres. Pode repetir (não é único) |
| `Valor` | Valor total: de 0,01 a 99.999.999,99, até 2 casas |
| `NumeroParcelas` | De 1 a 360 (`DividaValidador.MaximoParcelas`) |
| `DiaVencimento` | De 1 a 31 |
| `IdConta`, `IdCategoria` | Obrigatórias (a parcela exige as duas), ativas e do mesmo usuário |
| `IdCredor` | Opcional; se informado, ativo e do mesmo usuário |

**Geração das parcelas:**

- `Divida.Valor` é o **valor total**. Cada parcela recebe `Valor / NumeroParcelas`, truncado
  em 2 casas; **os centavos que sobrarem vão para a última parcela**
  (R$ 100 em 3x → 33,33 · 33,33 · 33,34).
- 1ª parcela: vence em `DataPrimeiroVencimento` (só a data, sem hora).
- Demais: nos meses seguintes, no dia `DiaVencimento`. Se o mês não tem esse dia, vence no
  último dia do mês (dia 31 → 28/02, 31/03, 30/04...).
- Descrição: `"<Nome> <n>/<total>"` (ex.: `Notebook 3/12`). Se passar de 100 caracteres, o nome é cortado.
- Conta e categoria das parcelas = as da dívida. As parcelas nascem ativas e sem pagamento.
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
- **As parcelas pagas nunca mudam** de valor, vencimento, conta ou categoria (só a descrição, abaixo).

**Campos simples** (nome, credor, conta, categoria), sem mudar nenhum campo financeiro: as parcelas continuam
as mesmas. A descrição de todas é refeita com o novo nome (`<Nome> <n>/<total>`, na ordem de vencimento); a
nova conta e categoria valem **só para as em aberto**.

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
- **O saldo das contas não muda**: o que já foi pago continua descontado.
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
A parcela não tem `id_usuario`: o dono é conferido **pela dívida** (`Divida.IdUsuario`). Regras comuns em
`ParcelaValidador`:

- Inexistente ou de outro usuário → `KeyNotFoundException`. Parcela ou dívida excluída → `RegraDeNegocioException`.
- **Parcela paga não muda**: não pode ser alterada nem excluída.
- Depois de incluir, alterar ou excluir, a dívida é **recalculada** a partir das parcelas ativas: valor total = soma delas,
  número de parcelas = quantidade, primeiro vencimento = o da primeira, e as descrições refeitas na ordem de
  vencimento (`<Nome> <n>/<total>`; no empate de vencimento, a recém-incluída fica por último). O dia de vencimento
não muda.

| Caso de uso | Assinatura | Regras |
|---|---|---|
| `BuscarParcelaPorIdUseCase` | `ExecutarAsync(id, idUsuario)` → `Parcela?` | Com `Conta`, `Categoria` e `Divida` (com as parcelas ativas dela, por vencimento, para a tela saber o número). `null` se não existe, é de outro usuário ou se a parcela ou a dívida foi excluída |
| `IncluirParcelaUseCase` | `ExecutarAsync(Parcela, idUsuario)` → `Parcela` | Na dívida `IdDivida` (ativa e do usuário: senão `KeyNotFoundException`/`RegraDeNegocioException`). Grava só valor, vencimento (só a data), conta e categoria: nasce ativa e **em aberto**. Recusa passar de 360 parcelas. Recalcula a dívida. O saldo não muda |
| `AlterarParcelaUseCase` | `ExecutarAsync(Parcela, idUsuario)` | Só parcela em aberto. Grava valor, vencimento (só a data), conta e categoria e recalcula a dívida |
| `ExcluirParcelaUseCase` | `ExecutarAsync(id, idUsuario)` | Só parcela em aberto; desativa (não apaga) e recalcula a dívida. **A única parcela ativa não pode ser excluída** (exclua a dívida). O saldo não muda |
| `PagarParcelaUseCase` | `ExecutarAsync(id, dataPagamento, idUsuario)` | Ver abaixo |

**Validação do `IncluirParcelaUseCase` e do `AlterarParcelaUseCase`** (`ParcelaValidador.ValidarCamposAsync`) (`ValidacaoException`, um erro por campo; nada gravado):

| Campo | Regra |
|---|---|
| `Valor` | De 0,01 a 99.999.999,99, até 2 casas. O novo total da dívida (soma das parcelas) também não pode passar de 99.999.999,99 |
| `IdConta`, `IdCategoria` | Obrigatórias, ativas e do mesmo usuário |
| `DataVencimento` | Livre. Pode mudar a ordem das parcelas: a numeração das descrições acompanha |

Se depois a **dívida** for alterada num campo financeiro, o `AlterarDividaUseCase` refaz as parcelas em aberto
pela regra da inclusão, e os ajustes feitos à mão nelas se perdem.

#### PagarParcelaUseCase

Registra o pagamento (ou recebimento) de uma parcela e ajusta o saldo da conta dela.

**Repositórios:** Parcelas, Dividas, Contas

**Validações:** parcela do usuário (pela dívida), ativa e ainda não paga; a dívida e a conta devem estar ativas;
a data do pagamento não pode ser futura (`ValidacaoException`, campo `DataPagamento`).

**Efeito:**

| `Divida.EhDivida` | Saldo da conta da parcela |
|---|---|
| `true` (dívida) | `Saldo -= Parcela.Valor` |
| `false` (crédito) | `Saldo += Parcela.Valor` |

- `Parcela.DataPagamento = dataPagamento` (só a data).
- Saldo negativo é permitido.

### RegistrarOperacaoUseCase

Registra a compra ou venda de um investimento e atualiza a quantidade em carteira.

**Repositórios:** Investimentos, Operacoes

**Validações:** `Quantidade` > 0; `ValorTotal` ≥ 0; o investimento deve existir
(`KeyNotFoundException`) e estar ativo; na venda, `Quantidade` ≤ quantidade disponível.

**Efeito:** compra → `Investimento.Quantidade += Quantidade`; venda → `-=`. A operação é gravada.
A cotação do investimento **não** é alterada.

`Operacao.ValorTotal` é o **valor total** da operação (quantidade × preço unitário), não o
preço unitário. Quando for preciso o preço unitário (preço médio, rentabilidade), calcule
`ValorTotal / Quantidade`.

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
- `PagarParcelaUseCase` ainda **não** grava histórico (pendência em [06-Pendencias.md](06-Pendencias.md)).
- O histórico não impede excluir a conta. Ao excluir, ele deve ser desativado junto (pendência em
  [06-Pendencias.md](06-Pendencias.md)).

---

## Catálogo — candidatos futuros

Operações já identificadas que envolvem mais de um repositório. Especifique aqui antes de implementar.

| Caso de uso | Envolve | Decisões em aberto |
|---|---|---|
| `EstornarPagamentoParcelaUseCase` | Parcelas, Dividas, Contas | Precisa de um método no repositório para limpar `DataPagamento`; reverte o saldo com o sinal inverso de `PagarParcela` |
| `TransferirEntreContasUseCase` | Contas | Uma só tabela, mas duas contas precisam mudar juntas |
| `RegistrarHistoricoUseCase` | Investimentos, Historicos | Retrato da carteira inteira ou de um investimento; periodicidade (manual ou mensal) |
| `DesativarOperacaoUseCase` | Operacoes, Investimentos | Reverter a quantidade do investimento; impedir se a quantidade ficar negativa |
