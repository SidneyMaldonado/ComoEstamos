# ComoEstamos

App de finanças pessoais em .NET MAUI (Android, iOS, Windows) com SQLite (EF Core). Textos do app, do código,
dos commits e do Spec em **português**.

## Antes de implementar: leia o Spec

As decisões de arquitetura e as regras de negócio ficam em [`Spec/`](Spec/README.md). Leia o documento do assunto
antes de criar ou mudar algo, e **atualize o Spec no mesmo commit** quando a implementação mudar uma decisão.

| Assunto | Documento |
|---|---|
| Camadas (MAUI → Core → Data) e o que vai em cada uma | `Spec/01-Arquitetura.md` |
| Repositórios e unidade de trabalho | `Spec/02-UnitOfWork.md` |
| Casos de uso: convenções e regras de cada um (dívidas, parcelas, backup...) | `Spec/03-UseCases.md` |
| Testes (InMemory, sem fakes; exceção do backup) | `Spec/04-Testes.md` |
| Telas, navegação, gestos e visual | `Spec/05-Telas.md` |
| O que falta fazer ou decidir | `Spec/06-Pendencias.md` |
| Estrutura de todo CRUD (página → service → 5 casos de uso → repositório) | `Spec/07-Crud.md` |

Para uma entidade nova, o usuário pode preencher `ComoEstamos/md/criar_tela_crud_modelo.md`; se faltar
informação, pergunte antes de implementar.

## Regras que valem em todo o código

- **Dívida e receita na mesma tabela.** `tb_divida` (`Divida`) guarda as duas: `EhDivida = true` é dívida (a pagar),
  `false` é **receita** (crédito, a receber). Na interface, use "receita". As telas de dívida e de receita
  **nunca mostram nem abrem** registros do outro tipo: toda consulta ou página nova filtra pelo tipo
  (`FiltroDividas.EhDivida`, checagem de `EhDivida` nas páginas).
- **Pagar parcela não mexe no saldo** da conta; o saldo muda só em "Atualizar Saldos" e no CRUD de Conta.
- **Exclusão é lógica** (`dm_ativo = false`); nada é apagado do banco.
- **Migrations:** nada destrutivo sem converter os dados antes (ver `Spec/06-Pendencias.md`).

## Fluxo de trabalho

- Cada trabalho numa **branch própria** a partir do `master` (`feature/...`, `docs/...`).
- **Commit e PR só quando o usuário pedir.** A PR descreve o que mudou, os testes e o que falta testar no app.
- Depois que o usuário avisar do merge: confira que a PR está **MERGED** antes de apagar qualquer coisa; então volte
  para o `master`, `git pull` e apague a branch local e a remota.
- Antes de entregar: `dotnet test ComoEstamos.Test` e o build do app
  (`dotnet build ComoEstamos/ComoEstamos.csproj -f net10.0-windows10.0.19041.0`; para Android, `-f net10.0-android`).
- O `.gitignore` padrão do Visual Studio ignora pastas `Backup*/`; há uma exceção para `ComoEstamos.Core/UseCases/Backup/`.
