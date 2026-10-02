# Especificações do ComoEstamos

Decisões de arquitetura e regras de negócio que valem para as próximas implementações.
Antes de criar uma funcionalidade nova, leia o documento correspondente. Se a
implementação exigir uma decisão diferente, atualize o documento no mesmo commit.

| Documento | Conteúdo |
|---|---|
| [01-Arquitetura.md](01-Arquitetura.md) | Projetos da solução, direção das dependências e o que vai em cada camada |
| [02-UnitOfWork.md](02-UnitOfWork.md) | Como os repositórios funcionam avulsos e dentro de uma unidade de trabalho |
| [03-UseCases.md](03-UseCases.md) | Quando criar um caso de uso, convenções e catálogo com as regras de cada um |
| [04-Testes.md](04-Testes.md) | Como testar repositórios e casos de uso |
| [05-Telas.md](05-Telas.md) | Telas MAUI: navegação, usuário atual, padrão de CRUD e cores |
| [06-Pendencias.md](06-Pendencias.md) | O que falta fazer ou decidir na próxima etapa |

## Glossário

| Termo | Significado |
|---|---|
| Dívida | Registro em `tb_divida` com `EhDivida = true`: valor **a pagar** |
| Crédito | Registro em `tb_divida` com `EhDivida = false`: valor **a receber** |
| Parcela | Cada vencimento de uma dívida ou crédito |
| Desativar | Exclusão lógica (`dm_ativo = 0`). Nada é apagado do banco |
