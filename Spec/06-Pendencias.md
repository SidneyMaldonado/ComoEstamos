# Pendências (próxima etapa)

Atualizado em 13/10/2026. Ao resolver um item, remova-o daqui e registre a decisão no documento correspondente.

## Git

- [ ] **`feature/operacao-valor-total`** (commit `8cb32e0`, renomeia `Operacao.Valor` → `ValorTotal`):
  commitada, mas sem push e sem PR. Foi criada antes do PR #3; fazer rebase sobre o `master`, conferir a
  migration e o snapshot do EF e abrir o PR.

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
  pagamento, desativar/alterar dívida, transferir entre contas, registrar histórico, desativar operação.
  Cada um tem decisões em aberto. Os CRUDs de Dívida e Operação dependem deles.

## Regras a implementar

- [ ] **Excluir conta desativa o histórico de saldo.** Ao excluir uma conta (exclusão lógica no
  `ExcluirContaUseCase`), os registros dela em `tb_historico_saldo` também passam a `dm_ativo = false`,
  na mesma unidade de trabalho. O histórico continua **sem impedir** a exclusão da conta. Precisa de um
  método no `HistoricoSaldoRepository` (ex.: `DesativarPorContaAsync`) e dos testes correspondentes.

## Telas

- [ ] **Telas das entidades fora do CRUD padrão**, na estrutura de [07-Crud.md](07-Crud.md)
  (página → service → caso de uso → repositório):

  | Entidade | Observação |
  |---|---|
  | Dívida | Inclusão pelo `CriarDividaUseCase`; alterar e excluir dependem de casos de uso em aberto |
  | Operação | Inclusão pelo `RegistrarOperacaoUseCase`; excluir depende de `DesativarOperacaoUseCase` |
  | Parcela | Sem inclusão própria; tela de pagamento (`PagarParcelaUseCase`) |
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
- [ ] **Android:** aviso `CS8602` em `Platforms/Android/MainActivity.cs` (linha 27), anterior a 13/10/2026.

## Limpeza

- [ ] Apagar `ComoEstamos.Test/Test1.cs`, o teste vazio do template.
- [ ] `ComoEstamos/sql/init_db.sql` é um script de SQL Server desatualizado (o banco é criado pelas
  migrations do SQLite). Apagar ou mover para uma pasta de referência.
