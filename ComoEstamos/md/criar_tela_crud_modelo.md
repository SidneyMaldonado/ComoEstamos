# Modelo: CRUD de uma entidade

Modelo genérico, tirado de `criar_tela_contas.md` e do que foi decidido na tela de Contas.

**Como usar:**

1. Copie este arquivo para `criar_tela_<entidade>.md` (ex.: `criar_tela_carteiras.md`).
2. Não altere a **Parte 1**: ela vale para todas as entidades.
3. Preencha a **Parte 2**, trocando tudo o que está entre `⟨ ⟩`. Apague as linhas que não se aplicam.
4. Peça a implementação apontando o arquivo preenchido. Se algum item da Parte 2 ficar em branco,
   a implementação deve perguntar antes de começar.

A estrutura de todo CRUD (página → service → 5 casos de uso → repositório) está em `Spec/07-Crud.md`,
e as convenções das telas em `Spec/05-Telas.md`. A referência de código é o CRUD de Contas
(`Pages/Contas/`, `ComoEstamos.Core/UseCases/Contas/`, `ComoEstamos.Core/Services/ContaService.cs`).

---

## Parte 1: regras fixas (valem para todo CRUD)

### Dados

- A exclusão é **sempre lógica**: grava `dm_ativo = false`. Nada é apagado do banco.
- As listagens mostram **só registros com `dm_ativo = true`**. Isso também vale para as listas de
  escolha (Picker) de chaves estrangeiras.
- Significado dos prefixos das colunas:

  | Prefixo | Significado | Exemplo |
  |---|---|---|
  | `id_` | Identificador ou chave estrangeira | `id_conta`, `id_usuario` |
  | `nm_` | Nome | `nm_conta` |
  | `ds_` | Descrição ou texto livre | `ds_observacao` |
  | `nr_` | Número (quantidade, valor, saldo) | `nr_saldo`, `nr_parcelas` |
  | `vl_` | Valor em dinheiro | `vl_cotacao` |
  | `dt_` | Data | `dt_operacao` |
  | `dia_` | Dia do mês (1 a 31) | `dia_vencimento` |
  | `dm_` | Domínio (booleano) | `dm_ativo`, `dm_compra` |
  | `img_` | Imagem (binário) | `img_conta` |

- **Usuário:** se a tabela tem `id_usuario`, ele **não aparece no formulário**. O valor vem de
  `IUsuarioAtual.ObterIdAsync()` (usuário local padrão, até existir login), informado pelo service aos
  casos de uso. As listas filtram por esse usuário (`ListarPorUsuarioAsync`).
- **Não aparecem no formulário:** `id_` da própria entidade, `dm_ativo`, `dt_criacao`, `dt_alteracao`
  (preenchidas pelo `AppDbContext`) e `img_` (imagens ficam de fora até decidirmos o seletor).
- **Quem grava:** sempre página → service → caso de uso → repositório (`Spec/07-Crud.md`), mesmo quando
  a operação mexe em uma tabela só. Os 5 casos de uso padrão são incluir, alterar, excluir, buscar todos e
  buscar por Id. Se a entidade precisa de um caso de uso diferente do padrão (ex.: a inclusão gera outras
  tabelas), ele precisa ser especificado em `Spec/03-UseCases.md` antes.
- **Domínio:** todo campo do formulário tem o domínio validado no caso de uso (tamanho, faixa, casas
  decimais, chave estrangeira ativa e do mesmo usuário, nome único entre os ativos).

### Telas

São **4 páginas independentes** por entidade, em `Pages/⟨Entidades⟩/`. A exclusão não tem página:
é um popup de confirmação na Visualizar.

| Página | Arquivo | Função |
|---|---|---|
| Listar | `Listar⟨Entidades⟩Page` | Lista dos registros ativos |
| Visualizar | `Visualizar⟨Entidade⟩Page` | Campos só de leitura; lápis (alterar) e lixeira (excluir) |
| Incluir | `Incluir⟨Entidade⟩Page` | Formulário de inclusão |
| Alterar | `Alterar⟨Entidade⟩Page` | O mesmo formulário, preenchido |

**Visual (igual em todas as páginas):**

- A mesma imagem de fundo (`fundo.png`) e a paleta de `Colors.xaml` (dourado sobre azul).
- Título dourado no topo e uma linha dourada abaixo dele.

**Listar:**

- Tabela `Controls/TabelaRegistros` (a mesma da MainPage): coluna de texto, com um detalhe opcional
  abaixo, e coluna de valor opcional.
- Quando não há registros, mostra "Nenhum(a) ⟨entidade⟩ cadastrado(a)."
- Tocar numa linha abre a página **Visualizar** com aquele registro.
- No canto superior direito, um **sinal de mais dourado sobre um círculo**, que abre a página **Incluir**.
- No canto superior esquerdo, o ícone **☰** dourado, que abre o menu lateral.
- A lista recarrega sempre que a página aparece, para refletir inclusões, alterações e exclusões.

**Incluir:**

- Título "Incluir ⟨Entidade⟩".
- Formulário com os campos da Parte 2. Os títulos dos campos ficam em **português correto**
  (`nm_conta` → "Nome da Conta"), nunca o nome da coluna.
- Botões **Cancelar** (contorno dourado) e **Salvar** (fundo dourado) no fim do formulário.
  - Cancelar volta para a lista, sem gravar.
  - Salvar valida, inclui no banco e volta para a lista.
- Antes de gravar, **valida todos os campos**. Cada campo errado mostra a mensagem logo abaixo dele,
  em vermelho claro, e nada é gravado.

**Visualizar:**

- Título "⟨Entidade⟩". Mostra cada campo (título e valor), mais "Data de Cadastro" e "Última Alteração".
- Recarrega sempre que aparece. Se o registro não existir mais ou estiver inativo, avisa e volta.
- No canto superior direito, um **lápis** (abre a Alterar) e uma **lixeira** dourados sobre círculos.
  A lixeira abre um popup: "Confirma a exclusão de ⟨entidade⟩ "⟨nome⟩"?"
  - **Sim**: exclui (`dm_ativo = false`) e volta para a lista. Se houver dependentes ativos, mostra a
    mensagem da Parte 2 e fica na Visualizar.
  - **Não**: fecha o popup e continua na Visualizar.
- Botão **Voltar** no fim.

**Alterar:**

- Título "Alterar ⟨Entidade⟩", o mesmo formulário do Incluir, preenchido.
- Carrega os dados do registro. Se ele não existir mais ou estiver inativo, avisa e volta.
- Salvar **atualiza** o registro e volta para a Visualizar. A validação é a mesma da inclusão.

### Menu lateral

- Toda entidade com CRUD ganha um item no **menu lateral** (`AppShell.xaml`), com ícone dourado
  e o título no plural, na posição indicada na Parte 2.
- O ícone é um SVG em `Resources/Images/⟨entidade⟩_icon.svg`: traço `#E8C25A`, 24×24, no mesmo
  estilo de `contas_icon.svg`.

### Controle por tipo de campo

| Tipo da coluna | Controle | Validação padrão |
|---|---|---|
| `nm_` / texto curto | `Entry` | Remove espaços nas pontas; obrigatório se `NOT NULL`; respeita o `MaxLength` |
| `ds_` / texto longo | `Editor` (3 a 4 linhas) | Respeita o `MaxLength` |
| Dinheiro (`nr_valor`, `vl_`, `nr_saldo`) | `Entry` numérico | Formato `1.250,00` (aceita o ponto decimal do teclado); casas e limite conforme o `Precision` |
| Quantidade (`nr_quantidade`) | `Entry` numérico | Casas conforme o `Precision` (ex.: 6); positiva, salvo indicação |
| Inteiro (`nr_parcelas`, `dia_`) | `Entry` numérico | Inteiro, dentro da faixa indicada |
| Data (`dt_`) | `DatePicker` (`dd/MM/yyyy`) | Só a data, sem hora |
| Booleano (`dm_`, exceto `dm_ativo`) | Dois `RadioButton` com os textos de cada opção (ex.: "Compra" / "Venda") | Uma das opções é obrigatória |
| Chave estrangeira (`id_`, exceto `id_usuario`) | `Picker` com o nome dos registros ativos do usuário | Obrigatória se `NOT NULL`; se opcional, a primeira opção é "(nenhum)" |

Na lista e na exibição, valores em dinheiro usam `R$` no formato pt-BR, e valores negativos aparecem em vermelho claro.

### Checklist de implementação

Siga o checklist completo de `Spec/07-Crud.md` (Data, Core, Testes e MAUI). Em resumo:

- [ ] Core: `⟨Entidade⟩Validador`, os 5 casos de uso e o `⟨Entidade⟩Service`, registrados em `AddComoEstamosCore`
- [ ] Data: métodos `ExisteAtivo...Por⟨Entidade⟩Async` nos repositórios dos dependentes, se houver
- [ ] Testes: `UseCaseTests/Crud⟨Entidade⟩Tests.cs`, `ServiceTests/⟨Entidade⟩ServiceTests.cs` e os dos métodos novos de repositório
- [ ] MAUI: as 4 páginas em `Pages/⟨Entidades⟩/`, rotas, menu, ícone e `AddTransient`
- [ ] `Spec/05-Telas.md`: regras do formulário da entidade
- [ ] Compilar sem avisos, rodar os testes e testar no app: incluir, validar, visualizar, alterar, cancelar,
  excluir (Sim e Não), exclusão impedida e menu

---

## Parte 2: dados da entidade (preencher)

### Identificação

| Item | Valor |
|---|---|
| Entidade (classe) | `⟨Carteira⟩` |
| Tabela | `⟨tb_carteira⟩` |
| Nome no singular / plural, como aparece na tela | ⟨Carteira⟩ / ⟨Carteiras⟩ |
| Gênero (para "Nenhuma … cadastrada", "Incluir …") | ⟨feminino / masculino⟩ |
| Posição no menu | ⟨depois de Contas⟩ |
| Ícone do menu (descrição) | ⟨uma carteira / maleta⟩ |

### Formulário

Liste **todas** as colunas da tabela. Na coluna "No formulário?", marque "não" para as que ficam fora (veja a Parte 1).

| Coluna | Título na tela | No formulário? | Controle | Obrigatório | Regras / faixa | Valor inicial na inclusão |
|---|---|---|---|---|---|---|
| `⟨nm_carteira⟩` | ⟨Nome da Carteira⟩ | sim | Entry | sim | ⟨até 50 caracteres⟩ | ⟨vazio⟩ |
| `⟨...⟩` | | | | | | |

**Campos que só podem ser informados na inclusão** (ficam bloqueados na alteração):
⟨nenhum / lista⟩

### Chaves estrangeiras

| Coluna | Escolhe entre | Texto exibido no Picker | Filtro extra |
|---|---|---|---|
| `⟨id_carteira⟩` | ⟨Carteiras ativas do usuário⟩ | ⟨Nome⟩ | ⟨nenhum⟩ |

### Lista

| Item | Valor |
|---|---|
| Colunas da tabela (título → campo) | ⟨Carteira → Nome⟩ · ⟨Investimentos → quantidade de investimentos ativos⟩ |
| Alinhamento | ⟨texto à esquerda, números/valores à direita⟩ |
| Ordenação | ⟨por Nome⟩ |
| Filtro | ⟨só ativos do usuário (padrão)⟩ |
| Linha de total no fim? | ⟨não⟩ |

### Casos de uso

| Operação | Caso de uso |
|---|---|
| Incluir | ⟨padrão (`Incluir⟨Entidade⟩UseCase`) / específico: `⟨Nome⟩UseCase`⟩ |
| Alterar | ⟨padrão / específico⟩ |
| Excluir | ⟨padrão / específico⟩ |

### Exclusão

- O que acontece com os registros que dependem deste? ⟨impedir a exclusão se houver dependentes ativos (padrão) / outro⟩
- Mensagem quando a exclusão é impedida: ⟨"Não é possível excluir: existem investimentos nesta carteira."⟩

### Validações extras

⟨Regras que vão além do tipo do campo e do nome único (que é padrão). Ex.: "a data não pode ser futura".
Escreva "nenhuma" se não houver.⟩

---

## Exemplo preenchido: Carteira

| Item | Valor |
|---|---|
| Entidade / Tabela | `Carteira` / `tb_carteira` |
| Singular / plural / gênero | Carteira / Carteiras / feminino |
| Menu | Depois de Contas; ícone de maleta |

| Coluna | Título na tela | No formulário? | Controle | Obrigatório | Regras | Valor inicial |
|---|---|---|---|---|---|---|
| `id_carteira` | — | não | — | — | — | — |
| `id_usuario` | — | não (usuário atual) | — | — | — | — |
| `nm_carteira` | Nome da Carteira | sim | Entry | sim | até 50 caracteres | vazio |
| `dm_ativo` | — | não | — | — | — | — |

- **Lista:** Carteira (Nome), ordenada por Nome, sem total.
- **Casos de uso:** os 5 padrão.
- **Exclusão:** impedir se houver investimentos ativos na carteira. Mensagem: "Não é possível excluir:
  existem investimentos ativos nesta carteira."
- **Validações extras:** nenhuma (o nome único é padrão).

Implementado em 07/10/2026, junto com Conta, Categoria, Credor e Investimento.

### Lembretes para as outras entidades

- **Divida:** a inclusão **gera as parcelas**, então usa o `CriarDividaUseCase`. Alterar e excluir ainda
  não têm caso de uso (`AlterarDividaUseCase`, `DesativarDividaUseCase` estão em aberto no
  `Spec/03-UseCases.md`) e precisam ser decididos antes. `dm_divida` vira os botões "Dívida" / "Crédito".
- **Operacao:** a inclusão atualiza a quantidade do investimento, então usa o `RegistrarOperacaoUseCase`.
  O campo de valor é o **valor total** da operação (`ValorTotal`), não o preço unitário.
- **Parcela:** é gerada pela dívida. Normalmente não tem tela de inclusão própria, só a de pagamento
  (`PagarParcelaUseCase`).
- **Historico:** é um retrato gerado pelo sistema (`RegistrarHistoricoUseCase`, em aberto). Normalmente
  só tem tela de consulta.
