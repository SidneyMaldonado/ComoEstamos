# Pendências (próxima etapa)

Atualizado em 02/10/2026. Ao resolver um item, remova-o daqui e registre a decisão no documento correspondente.

## Git

- [ ] **`feature/operacao-valor-total`** (commit `8cb32e0`, renomeia `Operacao.Valor` → `ValorTotal`):
  commitada, mas sem push e sem PR. Precisa ser juntada ao `master`.
- [ ] **`feature/main-page`** (MainPage, CRUD de Contas, menu lateral, modelo de CRUD): commitada,
  mas sem push e sem PR.

## Decisões em aberto

- [ ] **Login.** Hoje todas as telas usam o usuário local padrão (`IUsuarioAtual`, ver [05-Telas.md](05-Telas.md)).
  Falta definir se haverá login, cadastro de usuário e troca de usuário. Quando houver, só o `UsuarioAtual` muda.
- [ ] **Imagens** (`img_conta`, `img_categoria`, `img_logo`, `img_usuario`): ficaram fora dos formulários.
  Falta definir o seletor (galeria ou arquivo), o tamanho máximo e onde a imagem aparece (lista, menu).
- [ ] **Nome repetido.** Hoje é possível ter duas contas ativas com o mesmo nome. Falta decidir se isso deve
  ser bloqueado (e em quais entidades).
- [ ] **Casos de uso candidatos** em [03-UseCases.md](03-UseCases.md#catálogo--candidatos-futuros): estornar
  pagamento, desativar/alterar dívida, transferir entre contas, registrar histórico, desativar operação.
  Cada um tem decisões em aberto. Os CRUDs de Dívida e Operação dependem deles.

## Telas

- [ ] **CRUD das outras entidades**, seguindo o modelo `ComoEstamos/md/criar_tela_crud_modelo.md`
  (uma ficha preenchida por entidade antes de implementar):

  | Entidade | Observação |
  |---|---|
  | Carteira | Exemplo já preenchido no modelo |
  | Categoria | CRUD simples |
  | Credor | CRUD simples (`ds_observacoes` em `Editor`) |
  | Investimento | Picker de Carteira |
  | Dívida | Inclusão pelo `CriarDividaUseCase`; alterar e excluir dependem de casos de uso em aberto |
  | Operação | Inclusão pelo `RegistrarOperacaoUseCase`; excluir depende de `DesativarOperacaoUseCase` |
  | Parcela | Sem inclusão própria; tela de pagamento (`PagarParcelaUseCase`) |
  | Histórico | Só consulta; depende de `RegistrarHistoricoUseCase` |

- [ ] **Generalizar a `TabelaContas`** quando a segunda tela precisar de uma tabela no mesmo estilo,
  em vez de copiá-la.
- [ ] **Validação e leitura de valores sem testes.** A validação do formulário e a leitura do saldo
  (`TentarLerValor` em `ContaPage.xaml.cs`) estão no code-behind, fora do alcance do `ComoEstamos.Test`.
  Avaliar levar para o Core (um validador ou utilitário de formato pt-BR) e testar, já que todas as telas vão usar.

## Testes no app

- [ ] **Android (e iOS):** até agora o app só foi testado no Windows. Conferir:
  - se o teclado numérico permite digitar vírgula e sinal de menos no campo de saldo;
  - o menu lateral com a barra de navegação escondida (o ☰ da página é o único jeito de abrir, além do gesto);
  - o popup de confirmação de exclusão.
- [ ] **Windows:** o Shell mostra um ☰ próprio na barra de título além do ☰ da página. Decidir se mantém os dois.

## Limpeza

- [ ] Apagar `ComoEstamos.Test/Test1.cs`, o teste vazio do template.
- [ ] `ComoEstamos/sql/init_db.sql` é um script de SQL Server desatualizado (o banco é criado pelas
  migrations do SQLite). Apagar ou mover para uma pasta de referência.
