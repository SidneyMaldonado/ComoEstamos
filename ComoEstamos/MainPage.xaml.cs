using ComoEstamos.Data;
using ComoEstamos.Pages;
using SkiaSharp;

namespace ComoEstamos
{
    public partial class MainPage : ContentPage
    {
        private readonly SaldoDatabase _db;
        private readonly Dictionary<string, (Label Label, string Titulo)> _campos;
        private Label? _labelAtivo;

        // Valores editáveis (contas)
        private decimal _bbCC = -26.98m;
        private decimal _bbPoup = 0m;
        private decimal _nubank = 1528.29m;
        private decimal _pagbank = 314.69m;
        private decimal _infinity = 851.80m;
        private decimal _itau = 651.72m;
        private decimal _faltaPagar = 777.03m;

        public MainPage(SaldoDatabase db)
        {
            _db = db;
            InitializeComponent();

            _campos = new Dictionary<string, (Label, string)>
            {
                { "bbCC",       (lblBBCC,       "BB Conta Corrente") },
                { "bbPoup",     (lblBBPoup,     "BB Poupança") },
                { "nubank",     (lblNubank,     "Nubank") },
                { "pagbank",    (lblPagbank,    "Pagbank") },
                { "infinity",   (lblInfinity,   "Infinity") },
                { "itau",       (lblItau,       "Itaú") },
                { "faltaPagar", (lblFaltaPagar, "Falta Pagar") },
            };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CarregarDoBancoAsync();
            AtualizarTela();
        }

        private async Task CarregarDoBancoAsync()
        {
            var dados = await _db.CarregarTodosAsync();
            if (dados.Count == 0) return;

            if (dados.TryGetValue("bbCC",       out var v)) _bbCC       = v;
            if (dados.TryGetValue("bbPoup",     out v))     _bbPoup     = v;
            if (dados.TryGetValue("nubank",     out v))     _nubank     = v;
            if (dados.TryGetValue("pagbank",    out v))     _pagbank    = v;
            if (dados.TryGetValue("infinity",   out v))     _infinity   = v;
            if (dados.TryGetValue("itau",       out v))     _itau       = v;
            if (dados.TryGetValue("faltaPagar", out v))     _faltaPagar = v;
        }

        private void AtualizarTela()
        {
            lblData.Text = DateTime.Now.ToString("dd/MMM", new System.Globalization.CultureInfo("pt-BR"));

            AplicarValor(lblBBCC,       _bbCC);
            AplicarValor(lblBBPoup,     _bbPoup,     zero: true);
            AplicarValor(lblNubank,     _nubank);
            AplicarValor(lblPagbank,    _pagbank);
            AplicarValor(lblInfinity,   _infinity);
            AplicarValor(lblItau,       _itau);
            AplicarValor(lblFaltaPagar, _faltaPagar);

            var total = _bbCC + _bbPoup + _nubank + _pagbank + _infinity + _itau;
            var saldo = total - _faltaPagar;

            AplicarValor(lblTotal, total);
            AplicarValor(lblSaldo, saldo);
        }

        private static void AplicarValor(Label label, decimal valor, bool zero = false)
        {
            if (valor == 0 && zero)
            {
                label.Text = "-";
                label.TextColor = Color.FromArgb("#B0C4DE");
                return;
            }
            label.Text = valor.ToString("N2", new System.Globalization.CultureInfo("pt-BR"));
            label.TextColor = valor < 0
                ? Color.FromArgb("#FF6B6B")
                : Color.FromArgb("#7CFC00");
        }

        private void OnValorTapped(object? sender, TappedEventArgs e)
        {
            if (e.Parameter is not string key || !_campos.TryGetValue(key, out var info))
                return;

            _labelAtivo = info.Label;
            lblTituloEdicao.Text = info.Titulo;
            entryValor.Text = string.Empty;
            painelEdicao.IsVisible = true;
            entryValor.Focus();
        }

        private void OnCancelarClicked(object? sender, EventArgs e)
        {
            painelEdicao.IsVisible = false;
            _labelAtivo = null;
        }

        private async void OnContasClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ContasPage));
        }

        private async void OnCompartilharClicked(object? sender, EventArgs e)
        {
            var imagePath = await GerarImagemTabelaAsync();
            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Como Estamos",
                File = new ShareFile(imagePath, "image/png")
            });
        }

        private Task<string> GerarImagemTabelaAsync()
        {
            var cultura = new System.Globalization.CultureInfo("pt-BR");
            var total = _bbCC + _bbPoup + _nubank + _pagbank + _infinity + _itau;
            var saldo = total - _faltaPagar;

            var linhas = new (string Label, string Valor, bool Destaque, bool Negativo)[]
            {
                ("Como Estamos", DateTime.Now.ToString("dd/MMM", cultura),         true,  false),
                ("BB Conta Corrente", FormatarValor(_bbCC),                         false, _bbCC < 0),
                ("BB Poupança",       _bbPoup == 0 ? "-" : FormatarValor(_bbPoup),  false, _bbPoup < 0),
                ("Nubank",            FormatarValor(_nubank),                       false, _nubank < 0),
                ("Pagbank",           FormatarValor(_pagbank),                      false, _pagbank < 0),
                ("Infinity",          FormatarValor(_infinity),                     false, _infinity < 0),
                ("Itaú",              FormatarValor(_itau),                         false, _itau < 0),
                ("Total",             FormatarValor(total),                         true,  total < 0),
                ("Falta Pagar",       FormatarValor(_faltaPagar),                   false, true),
                ("Saldo",             FormatarValor(saldo),                         true,  saldo < 0),
            };

            const int largura = 780;
            const int alturaLinha = 60;
            const int cabecalhoAltura = 70;
            const int padding = 20;
            const int separadorAltura = 2;
            int alturaTotal = padding + cabecalhoAltura + (linhas.Length - 1) * (alturaLinha + separadorAltura) + padding;

            using var surface = SKSurface.Create(new SKImageInfo(largura, alturaTotal));
            var canvas = surface.Canvas;

            // Fundo
            canvas.Clear(new SKColor(0x1A, 0x2E, 0x6E));

            // Borda arredondada
            using var paintBorda = new SKPaint { Color = new SKColor(0xFF, 0xD7, 0x00), IsAntialias = true, StrokeWidth = 3, Style = SKPaintStyle.Stroke };
            canvas.DrawRoundRect(new SKRoundRect(new SKRect(2, 2, largura - 2, alturaTotal - 2), 16), paintBorda);

            float y = padding;

            for (int i = 0; i < linhas.Length; i++)
            {
                var (label, valor, destaque, negativo) = linhas[i];
                float lineH = i == 0 ? cabecalhoAltura : alturaLinha;
                float fontSize = destaque ? 28 : 24;

                var typeface = destaque
                    ? SKTypeface.FromFamilyName(null, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                    : SKTypeface.Default;

                // Texto esquerda
                using var fontEsq = new SKFont(typeface, fontSize);
                using var paintEsq = new SKPaint
                {
                    Color = destaque ? SKColors.White : new SKColor(0xB0, 0xC4, 0xDE),
                    IsAntialias = true,
                };
                canvas.DrawText(label, padding + 10, y + lineH * 0.65f, SKTextAlign.Left, fontEsq, paintEsq);

                // Cor do valor
                SKColor corValor;
                if (i == 0)
                    corValor = new SKColor(0xFF, 0xD7, 0x00);
                else if (i == linhas.Length - 1)
                    corValor = saldo < 0 ? new SKColor(0xFF, 0x6B, 0x6B) : new SKColor(0xFF, 0xD7, 0x00);
                else if (negativo)
                    corValor = new SKColor(0xFF, 0x6B, 0x6B);
                else
                    corValor = new SKColor(0x7C, 0xFC, 0x00);

                using var fontDir = new SKFont(typeface, fontSize);
                using var paintDir = new SKPaint
                {
                    Color = corValor,
                    IsAntialias = true,
                };
                canvas.DrawText(valor, largura - padding - 10, y + lineH * 0.65f, SKTextAlign.Right, fontDir, paintDir);

                y += lineH;

                // Separador
                if (i < linhas.Length - 1)
                {
                    bool separadorDourado = i == 0 || i == 6 || i == 7 || i == linhas.Length - 2;
                    using var paintSep = new SKPaint
                    {
                        Color = separadorDourado ? new SKColor(0xFF, 0xD7, 0x00) : new SKColor(0x2A, 0x3F, 0x80),
                        StrokeWidth = separadorDourado ? 2 : 1,
                    };
                    canvas.DrawLine(0, y, largura, y, paintSep);
                    y += separadorAltura;
                }
            }

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 95);

            var path = Path.Combine(FileSystem.CacheDirectory, "comoestamos_tabela.png");
            using var stream = File.OpenWrite(path);
            data.SaveTo(stream);

            return Task.FromResult(path);
        }

        private static string FormatarValor(decimal valor) =>
            valor.ToString("N2", new System.Globalization.CultureInfo("pt-BR"));

        private async void OnConfirmarClicked(object? sender, EventArgs e)
        {
            if (_labelAtivo is null)
                return;

            var texto = (entryValor.Text ?? "0").Replace(".", "").Replace(",", ".");
            if (!decimal.TryParse(texto, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var novoValor))
            {
                novoValor = 0;
            }

            string chave;
            if      (_labelAtivo == lblBBCC)       { _bbCC       = novoValor; chave = "bbCC"; }
            else if (_labelAtivo == lblBBPoup)      { _bbPoup     = novoValor; chave = "bbPoup"; }
            else if (_labelAtivo == lblNubank)      { _nubank     = novoValor; chave = "nubank"; }
            else if (_labelAtivo == lblPagbank)     { _pagbank    = novoValor; chave = "pagbank"; }
            else if (_labelAtivo == lblInfinity)    { _infinity   = novoValor; chave = "infinity"; }
            else if (_labelAtivo == lblItau)        { _itau       = novoValor; chave = "itau"; }
            else if (_labelAtivo == lblFaltaPagar)  { _faltaPagar = novoValor; chave = "faltaPagar"; }
            else                                    { chave = string.Empty; }

            if (!string.IsNullOrEmpty(chave))
                await _db.SalvarAsync(chave, novoValor);

            painelEdicao.IsVisible = false;
            _labelAtivo = null;
            AtualizarTela();
        }
    }
}
