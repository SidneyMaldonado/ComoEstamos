# Pendências (próxima etapa)

Atualizado em 14/10/2026. Ao resolver um item, remova-o daqui e registre a decisão no documento correspondente.

## Decisões em aberto

- [ ] **Login.** Hoje todas as telas usam o usuário local padrão (`IUsuarioAtual`, ver [05-Telas.md](05-Telas.md)).
  Falta definir se haverá login, cadastro de usuário e troca de usuário. Quando houver, só o `UsuarioAtual` muda.
- [ ] **Imagens** (`img_conta`, `img_categoria`, `img_logo`, `img_usuario`): ficaram fora dos formulários
  (a alteração as preserva). Falta definir o seletor (galeria ou arquivo), o tamanho máximo e onde a
  imagem aparece (lista, menu).
- [ ] **Quantidade do investimento.** Hoje a quantidade é editável no CRUD de Investimento, porque ainda
  não há tela de Operação. Quando houver, decidir se a quantidade passa a mudar só pelas operações
  (`RegistrarOperacaoUseCase`) e fica só de leitura no formulário.
- [ ] **Casos de uso candidatos** em [03-UseCases.md](03-UseCases.md#catálogo--candidatos-futuros): estornar
  pagamento, transferir entre contas, registrar histórico, desativar operação.
  Cada um tem decisões em aberto. O CRUD de Operação depende deles.

## Atualização do app (migrations)

Hoje o banco fica em `FileSystem.AppDataDirectory`, que o sistema preserva nas atualizações, e o
`AplicarMigrations()` (em `MauiProgram`) roda as migrations pendentes ao abrir, sem tratamento de erro.

- [ ] **Cópia de segurança antes das migrations.** Ao abrir, se houver migration pendente
  (`GetPendingMigrations()`), gravar antes uma cópia do banco no estilo do `backup-antes-do-restore.zip`
  (ex.: `backup-antes-da-atualizacao.zip`, só o último), reaproveitando o `PacoteBackup` e o `IArquivoBanco`
  ([03-UseCases.md](03-UseCases.md#backup-e-restore-usecasesbackup)). Assim, uma migration que dê errado pode ser
  desfeita. Decidir: guardar só a última ou uma por versão; se a cópia falhar, abrir o app mesmo assim ou não.
- [ ] **Falha na migration ao abrir.** Hoje, se o `Migrate()` falhar, o app fecha ao abrir e o usuário fica sem
  acesso. Tratar a falha: mostrar uma tela com a mensagem e as opções de restaurar a cópia de antes da atualização
  (item acima) ou um backup escolhido (Configurações → Restaurar), em vez de fechar. Decidir o que a tela mostra e se
  dá para enviar o erro (ex.: compartilhar o texto do erro).
- Regra para as próximas migrations: nada destrutivo sem conversão dos dados (apagar ou renomear coluna de verdade
  perde o dado; renomear propriedade mantendo a coluna, como em `OperacaoValorTotal`, não perde).

## Telas

- [ ] **Senha no backup** (decidido: depois). Proteger o `.zip` com senha (AES; precisa de uma biblioteca de zip com
  criptografia). Até lá, a tela avisa ao compartilhar que o arquivo não tem senha.
- [ ] **Backup automático do sistema:** o `AndroidManifest` está com `android:allowBackup="true"`, então o Android
  (Auto Backup) já pode copiar os dados do app, inclusive o banco, para o Google Drive do usuário; o iOS faz o mesmo
  no iCloud. Decidir se mantém ou desliga (não substitui o backup manual). Se mantiver, conferir no aparelho se o
  banco volta inteiro numa reinstalação.
- [ ] **Telas das entidades fora do CRUD padrão**, na estrutura de [07-Crud.md](07-Crud.md)
  (página → service → caso de uso → repositório):

  | Entidade | Observação |
  |---|---|
  | Dívida | Listar, Visualizar, Incluir, Alterar e Excluir feitos em 13/10/2026 ([05-Telas.md](05-Telas.md#dívidas)). Receitas (`EhDivida = false`) com tela própria em 14/10/2026 ([05-Telas.md](05-Telas.md#receitas)) |
  | Operação | Inclusão pelo `RegistrarOperacaoUseCase`; excluir depende de `DesativarOperacaoUseCase` |
  | Parcela | Visualizar (com Pagar), Incluir, Alterar e Excluir feitos em 14/10/2026 ([05-Telas.md](05-Telas.md#parcelas)). Falta o **estorno** do pagamento (`EstornarPagamentoParcelaUseCase`) |
  | Histórico | Só consulta; depende de `RegistrarHistoricoUseCase` |

## Testes no app

- [ ] **Windows — CRUDs de 07/10/2026:** testado no app só o início do CRUD de Carteira (menu, lista vazia,
  validação de nome vazio, inclusão com espaços, nome repetido, segunda inclusão). Falta conferir nas 5
  entidades: visualizar, alterar (lápis), cancelar, excluir (Sim e Não), exclusão impedida por dependentes,
  o Picker de carteira do Investimento e os erros de formato nos campos numéricos.
- [ ] **Android (e iOS):** até agora o app só foi testado no Windows. Conferir:
  - se o teclado numérico permite digitar vírgula e sinal de menos nos campos de valor;
  - o menu lateral com a barra de navegação escondida (o ☰ da página é o único jeito de abrir, além do gesto);
  - os popups de confirmação de exclusão.
- [ ] **Windows:** o Shell mostra um ☰ próprio na barra de título além do ☰ da página. Decidir se mantém os dois.

- [ ] **Windows e Android — 13/10/2026:** testar no app o total da tela inicial, a tela "Atualizar Saldos"
  (Salvar, Cancelar, erro de formato e de faixa abaixo da conta, nada gravado quando há erro) e o
  compartilhamento da imagem dos saldos (no Android, conferir a fonte, que não é Calibri, e o app de destino).
- [ ] **Windows e Android — Dívidas (13/10/2026):** testar no app a lista (cada filtro, ✕ do mês, ocultar
  pagas, ordem, filtros mantidos ao voltar), a Visualizar (rolagem da tabela de parcelas em tela pequena) e a
  Incluir (Pickers, `DatePicker` preenchendo o dia, erros de formato e de domínio, parcelas geradas), a Alterar
  (sem e com parcelas pagas: aviso, primeiro vencimento bloqueado, erros de valor e de número de parcelas) e a
  exclusão (Sim e Não, mensagem extra quando há parcela paga). Na tela inicial, o atalho para as dívidas: arrastar
  para cima (Android: em cima da tabela, fora dela e com muitas contas) e tocar na dica; e a volta, arrastando para
  baixo na lista de dívidas.
- [ ] **Windows e Android — Resumo do Mês (14/10/2026):** testar no app o item do menu e o atalho da tela inicial
  (arrastar para cima e tocar na dica, agora para o resumo), do resumo para as dívidas (arrastar no título e na linha
  do mês, e tocar na dica no fim da rolagem), a volta na mesma ordem (dívidas → resumo → início), as setas e o toque no mês,
  a ida para os investimentos (arrastar para baixo na tela inicial) e a volta (arrastar para cima nos investimentos),
  a rolagem das três tabelas juntas (`RolaSozinha="False"`, em tela pequena e com muitas dívidas), o tom das pagas, os
  textos Paga/Vencida/Vence, a Diferença negativa em vermelho e o toque na linha abrindo a dívida. Conferir também
  que as tabelas das outras telas continuam rolando.
- [ ] **Windows e Android — Parcelas (14/10/2026):** testar no app o + da seção Parcelas (tamanho e alinhamento com o
  título; a Incluir preenchida, erros, a nova parcela na ordem certa e o total recalculado), o toque na parcela da Visualizar Dívida, a
  Visualizar Parcela (em aberto, vencida e paga: lápis e lixeira apagados), o Pagar (Sim e Não, data de hoje e
  anterior, saldo da conta sem mudar, página recarregada), a Alterar (valor, vencimento mudando a ordem, conta,
  sem campo de categoria, erros de formato e de domínio, total da dívida recalculado na volta) e a exclusão (Sim e Não, única
  parcela recusada, volta para a dívida com uma parcela a menos).
- [ ] **Windows e Android — Receitas (14/10/2026):** testar no app o item "Receitas" do menu (ícone), a lista (só
  receitas; filtros; "Recebida"), a Incluir (Pagador opcional, receita criada não aparece em Dívidas), a Visualizar e a
  Alterar (aviso de recebidas), a exclusão, e as parcelas de receita (título "Receita", botão Receber, saldo da conta
  sem mudar, "Recebida em"). Conferir que uma receita não abre nas telas de dívida (e vice-versa) e que, no Resumo do
  Mês, a linha de receita abre a Visualizar Receita. No Android, os atalhos: arrastar para cima nas dívidas abre as
  receitas, e para baixo nas receitas volta para as dívidas (sem atrapalhar a rolagem da tabela).
- [ ] **Windows e Android — Configurações/Backup (14/10/2026):** testar no app o item do menu (ícone), o aviso de
  "sem senha" (Sim e Não), o backup compartilhado (Drive, e-mail, WhatsApp; nome do arquivo; "Último backup"
  atualizado), o restore de um backup do Drive e de Downloads (seletor de arquivos, resumo, Sim e Não, dados trocados,
  volta para o Início com os novos saldos), os arquivos recusados (um `.zip` qualquer, um arquivo que não é zip) e o
  `backup-antes-do-restore.zip` na pasta do app.
- [ ] **Windows e Android — dono da dívida pela conta (16/10/2026):** num banco com dados, abrir o app (migrations
  `RemoverCategoriaParcela` e `RemoverUsuarioDivida`) e conferir que as dívidas, as receitas e as parcelas continuam
  na lista, no Resumo do Mês e na Visualizar; incluir e alterar dívida e parcela; restaurar um backup anterior a
  essas migrations.
- [ ] **Android:** aviso `CS8602` em `Platforms/Android/MainActivity.cs` (linha 27), anterior a 13/10/2026.

## Limpeza

- [ ] Apagar `ComoEstamos.Test/Test1.cs`, o teste vazio do template.
- [ ] `ComoEstamos/sql/init_db.sql` é um script de SQL Server desatualizado (o banco é criado pelas
  migrations do SQLite). Apagar ou mover para uma pasta de referência.
