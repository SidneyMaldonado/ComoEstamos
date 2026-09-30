# Unit of Work

Decisão: operações que alteram mais de um repositório são gravadas por uma
**unidade de trabalho** (`IUnitOfWork`, em `ComoEstamos.Data/UnitOfWork.cs`),
de forma atômica: tudo é gravado ou nada é.

## Os dois modos de um repositório

| | Avulso | Dentro de uma `IUnitOfWork` |
|---|---|---|
| Como obter | Injetando `ICarteiraRepository` etc. | `uow.Carteiras`, `uow.Parcelas`... |
| `DbContext` | Um novo por operação | O mesmo para todos os repositórios da unidade |
| Quando grava | Ao fim de cada operação | Somente em `uow.ConfirmarAsync()` |
| `ObterPorIdAsync` | Retorna a entidade **não rastreada** | Retorna a entidade **rastreada** e enxerga alterações pendentes |
| Listagens (`ListarAsync`, `ListarPor...`) | Leem o banco | Leem **só o que já foi gravado**; não veem alterações pendentes |
| Id de entidade inserida | Preenchido ao retornar de `InserirAsync` | Garantido só depois de `ConfirmarAsync` |

## Uso padrão (dentro de um caso de uso)

```csharp
await using var uow = await unitOfWorkFactory.IniciarAsync();

// 1. Carregar e validar. Lance a exceção antes de alterar qualquer coisa.
var parcela = await uow.Parcelas.ObterPorIdAsync(id) ?? throw new KeyNotFoundException(...);

// 2. Alterar pelos repositórios da unidade.
await uow.Parcelas.RegistrarPagamentoAsync(parcela.Id, data);
await uow.Contas.AtualizarSaldoAsync(conta.Id, novoSaldo);

// 3. Gravar tudo de uma vez.
await uow.ConfirmarAsync();
```

Se uma exceção for lançada antes de `ConfirmarAsync`, o `await using` descarta a
unidade e nada é gravado. Se `ConfirmarAsync` falhar (por exemplo, numa constraint),
o SQLite desfaz a gravação inteira.

## Regras

1. **Um caso de uso = uma unidade de trabalho = um `ConfirmarAsync`**, sempre no final.
2. **Nunca** misture um repositório avulso (injetado) com os da unidade na mesma operação:
   o avulso grava na hora e fica fora da atomicidade.
3. **Relacionar entidades novas pela navegação**, não pelo Id, porque o Id ainda não existe:
   `new Parcela { Divida = divida, ... }`. O EF preenche a chave estrangeira ao confirmar.
4. **Não use listagens para ler o que a própria unidade alterou.** Para isso, use `ObterPorIdAsync`
   ou guarde a referência da entidade.
5. Para alterar uma entidade dentro da unidade, prefira obtê-la com `uow.X.ObterPorIdAsync` e
   alterar a instância retornada, que já é rastreada. Passar para `AtualizarAsync` uma **outra
   instância** com o mesmo Id de uma entidade já rastreada gera erro do EF.
6. A unidade de trabalho não é thread-safe e é descartada ao fim da operação. Não a guarde em campo.

## Métodos novos em repositórios

Todo método novo em repositório deve funcionar nos dois modos. Use o contexto fornecido
pela base, nunca a factory diretamente:

```csharp
public async Task AlgumaAlteracaoAsync(int id)
{
    await using var ctx = await AbrirContextoAsync();
    var entidade = await ctx.Db.Xs.FindAsync(id) ?? throw new KeyNotFoundException(...);
    // ... alterações ...
    await ctx.SalvarAsync();   // nunca ctx.Db.SaveChangesAsync()
}
```

Todo repositório novo precisa de:

- um construtor público recebendo `IDbContextFactory<AppDbContext>` (modo avulso e DI);
- um construtor `internal` recebendo `AppDbContext` (modo unidade de trabalho);
- uma propriedade correspondente em `IUnitOfWork`/`UnitOfWork`;
- o registro em `AddComoEstamosData`.
