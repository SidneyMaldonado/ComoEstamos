using System.Globalization;
using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;
using ComoEstamos.Services;

namespace ComoEstamos.Pages
{
    /// <summary>
    /// Inclusão e alteração de conta. Sem o parâmetro <c>id</c> a página inclui;
    /// com ele, carrega a conta, altera e permite excluir (desativar).
    /// </summary>
    public partial class ContaPage : ContentPage, IQueryAttributable
    {
        public const string Rota = "conta";

        private const int TamanhoMaximoNome = 100;
        // nr_saldo é decimal(10,2).
        private const decimal SaldoMaximo = 99_999_999.99m;
        private static readonly CultureInfo PtBr = new("pt-BR");

        private readonly IContaRepository contaRepository;
        private readonly IUsuarioAtual usuarioAtual;

        private int? idConta;
        private Conta? conta;
        private bool carregada;

        public ContaPage(IContaRepository contaRepository, IUsuarioAtual usuarioAtual)
        {
            this.contaRepository = contaRepository;
            this.usuarioAtual = usuarioAtual;
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("id", out var valor) && int.TryParse(valor?.ToString(), out var id))
                idConta = id;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (carregada)
                return;
            carregada = true;

            if (idConta is not int id)
                return;

            Titulo.Text = "Alterar Conta";
            BotaoExcluir.IsVisible = true;

            conta = await contaRepository.ObterPorIdAsync(id);
            if (conta is null || !conta.Ativo)
            {
                await DisplayAlertAsync("Conta não encontrada", "A conta não existe mais ou foi excluída.", "OK");
                await VoltarParaListaAsync();
                return;
            }

            CampoNome.Text = conta.Nome;
            CampoSaldo.Text = conta.Saldo.ToString("N2", PtBr);
        }

        private async void Salvar_Clicked(object? sender, EventArgs e)
        {
            if (!Validar(out var nome, out var saldo))
                return;

            BotaoSalvar.IsEnabled = false;
            try
            {
                if (conta is null)
                {
                    await contaRepository.InserirAsync(new Conta
                    {
                        IdUsuario = await usuarioAtual.ObterIdAsync(),
                        Nome = nome,
                        Saldo = saldo,
                    });
                }
                else
                {
                    conta.Nome = nome;
                    conta.Saldo = saldo;
                    await contaRepository.AtualizarAsync(conta);
                }
                await VoltarParaListaAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Não foi possível salvar", ex.Message, "OK");
            }
            finally
            {
                BotaoSalvar.IsEnabled = true;
            }
        }

        private async void Cancelar_Clicked(object? sender, EventArgs e) => await VoltarParaListaAsync();

        private async void Excluir_Tapped(object? sender, TappedEventArgs e)
        {
            if (conta is null)
                return;

            var confirmou = await DisplayAlertAsync(
                "Excluir conta", $"Confirma a exclusão da conta \"{conta.Nome}\"?", "Sim", "Não");
            if (!confirmou)
                return;

            try
            {
                await contaRepository.DesativarAsync(conta.Id);
                await VoltarParaListaAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Não foi possível excluir", ex.Message, "OK");
            }
        }

        /// <summary>Valida os campos e mostra a mensagem de cada um que estiver errado.</summary>
        private bool Validar(out string nome, out decimal saldo)
        {
            nome = CampoNome.Text?.Trim() ?? string.Empty;
            var erroNome = nome.Length switch
            {
                0 => "Informe o nome da conta.",
                > TamanhoMaximoNome => $"O nome deve ter no máximo {TamanhoMaximoNome} caracteres.",
                _ => null,
            };

            saldo = 0;
            string? erroSaldo = null;
            if (string.IsNullOrWhiteSpace(CampoSaldo.Text))
                erroSaldo = "Informe o saldo.";
            else if (!TentarLerValor(CampoSaldo.Text, out var lido))
                erroSaldo = "Saldo inválido. Use o formato 1.250,00.";
            else if (decimal.Round(lido, 2) != lido)
                erroSaldo = "O saldo deve ter no máximo 2 casas decimais.";
            else if (Math.Abs(lido) > SaldoMaximo)
                erroSaldo = $"O saldo deve estar entre -{SaldoMaximo.ToString("N2", PtBr)} e {SaldoMaximo.ToString("N2", PtBr)}.";
            else
                saldo = lido;

            MostrarErro(ErroNome, erroNome);
            MostrarErro(ErroSaldo, erroSaldo);
            return erroNome is null && erroSaldo is null;
        }

        private static void MostrarErro(Label label, string? mensagem)
        {
            label.Text = mensagem;
            label.IsVisible = mensagem is not null;
        }

        /// <summary>
        /// Lê um valor no formato brasileiro (1.250,50). Sem vírgula, aceita o ponto como separador
        /// decimal quando ele é único e seguido de até 2 dígitos (1250.5), como sai de alguns teclados numéricos.
        /// </summary>
        private static bool TentarLerValor(string texto, out decimal valor)
        {
            texto = texto.Trim().Replace("R$", "").Replace(" ", "");
            if (!texto.Contains(','))
            {
                var ponto = texto.LastIndexOf('.');
                var usaPontoDecimal = ponto >= 0 && texto.IndexOf('.') == ponto && texto.Length - ponto - 1 <= 2;
                if (usaPontoDecimal)
                    texto = texto.Replace('.', ',');
            }
            return decimal.TryParse(texto, NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, PtBr, out valor);
        }

        private static Task VoltarParaListaAsync() => Shell.Current.GoToAsync("..");
    }
}
