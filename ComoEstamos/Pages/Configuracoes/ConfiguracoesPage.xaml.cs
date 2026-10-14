using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Core.UseCases.Backup;

namespace ComoEstamos.Pages.Configuracoes
{
    /// <summary>
    /// Opções gerais do app. Por enquanto, só o backup (manual): gerar e compartilhar o arquivo, ou restaurar um
    /// backup, que substitui todos os dados atuais.
    /// </summary>
    public partial class ConfiguracoesPage : ContentPage
    {
        /// <summary>Data/hora do último backup gerado neste aparelho, em <see cref="Preferences"/> (só para exibir).</summary>
        private const string ChaveUltimoBackup = "ultimo_backup";

        /// <summary>Tipos aceitos no seletor de arquivos do restore (o conteúdo é conferido depois).</summary>
        private static readonly FilePickerFileType TipoZip = new(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            [DevicePlatform.Android] = ["application/zip", "application/x-zip-compressed", "application/octet-stream"],
            [DevicePlatform.iOS] = ["public.zip-archive"],
            [DevicePlatform.MacCatalyst] = ["public.zip-archive"],
            [DevicePlatform.WinUI] = [".zip"],
        });

        private readonly IBackupService backupService;

        public ConfiguracoesPage(IBackupService backupService)
        {
            this.backupService = backupService;
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            MostrarUltimoBackup();
        }

        private void MostrarUltimoBackup()
        {
            var ultimo = Preferences.Default.Get(ChaveUltimoBackup, DateTime.MinValue);
            TextoUltimoBackup.Text = ultimo == DateTime.MinValue
                ? "Nenhum backup feito neste aparelho."
                : $"Último backup: {DataHora(ultimo)}";
        }

        /// <summary>"14/10/2026 às 15:30".</summary>
        private static string DataHora(DateTime data) => $"{FormatoBr.Data(data)} às {data:HH:mm}";

        private async void Backup_Clicked(object? sender, EventArgs e)
        {
            // O arquivo ainda não tem senha (Spec/06-Pendencias.md): o usuário é avisado antes de compartilhar.
            var continuar = await DisplayAlertAsync("Fazer backup",
                "O arquivo de backup tem todos os seus dados financeiros e não é protegido por senha. " +
                "Compartilhe só com destinos de sua confiança (seu Drive, seu e-mail). Continuar?",
                "Sim", "Não");
            if (!continuar)
                return;

            await ExecutarAsync("Não foi possível fazer o backup", async () =>
            {
                var caminho = await backupService.GerarAsync(FileSystem.CacheDirectory, AppInfo.Current.VersionString);
                Preferences.Default.Set(ChaveUltimoBackup, DateTime.Now);
                MostrarUltimoBackup();

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Backup do Como Estamos",
                    File = new ShareFile(caminho, "application/zip"),
                });
            });
        }

        private async void Restaurar_Clicked(object? sender, EventArgs e)
        {
            var escolhido = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Escolha o arquivo de backup",
                FileTypes = TipoZip,
            });
            if (escolhido is null)
                return;

            await ExecutarAsync("Não foi possível restaurar", async () =>
            {
                // Cópia local: o arquivo escolhido pode estar num provedor (Drive, Downloads) sem caminho direto.
                var caminho = Path.Combine(FileSystem.CacheDirectory, "backup-escolhido.zip");
                await using (var origem = await escolhido.OpenReadAsync())
                await using (var destino = File.Create(caminho))
                    await origem.CopyToAsync(destino);

                try
                {
                    var resumo = await backupService.ValidarAsync(caminho);
                    var confirma = await DisplayAlertAsync("Restaurar backup", Pergunta(resumo), "Sim", "Não");
                    if (!confirma)
                        return;

                    await backupService.RestaurarAsync(caminho, AppInfo.Current.VersionString);
                }
                finally
                {
                    File.Delete(caminho);
                }

                await DisplayAlertAsync("Restaurar backup", "Backup restaurado.", "OK");
                await Shell.Current.GoToAsync("//MainPage");
            });
        }

        /// <summary>"Backup de 14/10/2026 às 15:30: 5 contas, 12 dívidas, ... Todos os dados atuais serão substituídos..."</summary>
        private static string Pergunta(ResumoBackup resumo)
        {
            var c = resumo.Contagens;
            return $"Backup de {DataHora(resumo.DataHora)}: {Quantidade(c.Contas, "conta", "contas")}, " +
                $"{Quantidade(c.Dividas, "dívida", "dívidas")}, {Quantidade(c.Receitas, "receita", "receitas")}, " +
                $"{Quantidade(c.Parcelas, "parcela", "parcelas")} e {Quantidade(c.Investimentos, "investimento", "investimentos")}." +
                Environment.NewLine + Environment.NewLine +
                "Todos os dados atuais serão substituídos por este backup. Confirma?";
        }

        private static string Quantidade(int n, string singular, string plural) => $"{n} {(n == 1 ? singular : plural)}";

        /// <summary>Roda a operação com os botões desabilitados e mostra o erro num alerta.</summary>
        private async Task ExecutarAsync(string tituloErro, Func<Task> operacao)
        {
            BotaoBackup.IsEnabled = BotaoRestaurar.IsEnabled = false;
            Carregando.IsVisible = Carregando.IsRunning = true;
            try
            {
                await operacao();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(tituloErro, ex.Message, "OK");
            }
            finally
            {
                BotaoBackup.IsEnabled = BotaoRestaurar.IsEnabled = true;
                Carregando.IsVisible = Carregando.IsRunning = false;
            }
        }

        private void Menu_Clicked(object? sender, EventArgs e) => Shell.Current.FlyoutIsPresented = true;
    }
}
