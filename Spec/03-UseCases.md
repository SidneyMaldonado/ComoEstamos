# Casos de uso

Ficam em `ComoEstamos.Core/UseCases/`, um arquivo por caso de uso.

## Quando criar

Crie um caso de uso quando a operação:

- altera **mais de um repositório** (precisa de atomicidade), ou
- tem **regra de negócio** que não é só persistência (cálculo, validação entre entidades).

CRUD simples de uma entidade continua indo direto ao repositório.

## Convenções

- Nome: `<Verbo><Objeto>UseCase`, com a interface `I<Verbo><Objeto>UseCase` no mesmo arquivo.
- Um único método público: `ExecutarAsync(...)`.
- Recebe `IUnitOfWorkFactory` no construtor e usa **uma** unidade de trabalho por execução
  (ver [02-UnitOfWork.md](02-UnitOfWork.md)).
- Ordem: **validar tudo → alterar → `ConfirmarAsync()`**. Nenhuma alteração antes da última validação.
- Erros:
  - `RegraDeNegocioException`: operação recusada por regra. A mensagem é mostrada ao usuário.
  - `KeyNotFoundException`: o Id principal recebido não existe (mesmo padrão dos repositórios).
- Registro em `ComoEstamos.Core/ServiceCollectionExtensions.cs` (`AddComoEstamosCore`), como singleton.
- Testes em `ComoEstamos.Test/UseCaseTests/<Nome>Tests.cs` (ver [04-Testes.md](04-Testes.md)).

---

## Catálogo — implementados

### CriarDividaUseCase

Cadastra uma dívida ou crédito e gera as parcelas.

**Repositórios:** Usuarios, Contas, Categorias, Credores (leitura) · Dividas, Parcelas (escrita)

**Validações** (qualquer falha → `RegraDeNegocioException`, nada gravado):

| Regra | |
|---|---|
| Nome | obrigatório |
| `DiaVencimento` | entre 1 e 31 |
| `NumeroParcelas` | ≥ 1 |
| `Valor` | > 0 |
| Conta e Categoria | obrigatórias (a parcela exige as duas), ativas e do mesmo usuário |
| Credor | opcional; se informado, deve estar ativo e ser do mesmo usuário |
| Usuário | existente e ativo |

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

### PagarParcelaUseCase

Registra o pagamento (ou recebimento) de uma parcela e ajusta o saldo da conta dela.

**Repositórios:** Parcelas, Dividas, Contas

**Validações:** a parcela deve existir (`KeyNotFoundException`), estar ativa e ainda não
estar paga; a dívida e a conta devem estar ativas.

**Efeito:**

| `Divida.EhDivida` | Saldo da conta da parcela |
|---|---|
| `true` (dívida) | `Saldo -= Parcela.Valor` |
| `false` (crédito) | `Saldo += Parcela.Valor` |

- `Parcela.DataPagamento = dataPagamento`.
- Saldo negativo é permitido.

### RegistrarOperacaoUseCase

Registra a compra ou venda de um investimento e atualiza a quantidade em carteira.

**Repositórios:** Investimentos, Operacoes

**Validações:** `Quantidade` > 0; `Valor` ≥ 0; o investimento deve existir
(`KeyNotFoundException`) e estar ativo; na venda, `Quantidade` ≤ quantidade disponível.

**Efeito:** compra → `Investimento.Quantidade += Quantidade`; venda → `-=`. A operação é gravada.
A cotação do investimento **não** é alterada.

---

## Catálogo — candidatos futuros

Operações já identificadas que envolvem mais de um repositório. Especifique aqui antes de implementar.

| Caso de uso | Envolve | Decisões em aberto |
|---|---|---|
| `EstornarPagamentoParcelaUseCase` | Parcelas, Dividas, Contas | Precisa de um método no repositório para limpar `DataPagamento`; reverte o saldo com o sinal inverso de `PagarParcela` |
| `DesativarDividaUseCase` | Dividas, Parcelas | Desativar também as parcelas em aberto? E as já pagas? |
| `AlterarDividaUseCase` | Dividas, Parcelas | O que fazer com as parcelas já geradas quando mudam valor, número de parcelas ou vencimento (regerar só as não pagas?) |
| `TransferirEntreContasUseCase` | Contas | Uma só tabela, mas duas contas precisam mudar juntas |
| `RegistrarHistoricoUseCase` | Investimentos, Historicos | Retrato da carteira inteira ou de um investimento; periodicidade (manual ou mensal) |
| `DesativarOperacaoUseCase` | Operacoes, Investimentos | Reverter a quantidade do investimento; impedir se a quantidade ficar negativa |

## Decisões em aberto

- **`Operacao.Valor`**: é o valor total da operação ou o preço unitário? Hoje nenhum caso de
  uso faz conta com ele; defina antes de calcular preço médio ou rentabilidade.
