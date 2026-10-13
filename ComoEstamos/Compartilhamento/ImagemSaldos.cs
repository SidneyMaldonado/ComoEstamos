using System.Globalization;
using SkiaSharp;

namespace ComoEstamos.Compartilhamento
{
    /// <summary>
    /// Gera o PNG da tabela de saldos compartilhada pela tela inicial, no visual de planilha:
    /// cabeçalho "Como Estamos" com a data (14/out), uma linha por conta e o total, com bordas pretas
    /// e cabeçalho e total em verde claro.
    /// </summary>
    internal static class ImagemSaldos
    {
        /// <summary>Pixels por ponto: gera a imagem em alta resolução para não borrar no celular.</summary>
        private const float Escala = 3f;

        private const float TamanhoFonte = 15f;
        private const float AlturaLinha = 22f;
        private const float Margem = 4f;
        private const float RespiroColunas = 24f;
        private const float LarguraMinimaNome = 170f;
        private const float LarguraMinimaValor = 90f;

        private static readonly SKColor Verde = SKColor.Parse("#C6E0B4");
        private static readonly SKColor Borda = SKColors.Black;
        private static readonly SKColor Texto = SKColors.Black;

        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");
        private static readonly string[] Meses = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];

        /// <summary>Data no formato do cabeçalho: "14/out".</summary>
        public static string DataCabecalho(DateTime data) => $"{data.Day:00}/{Meses[data.Month - 1]}";

        /// <summary>Valor como na planilha: separador de milhar e 2 casas, sem "R$" (1.447,75).</summary>
        public static string Valor(decimal valor) => valor.ToString("N2", Cultura);

        public static byte[] GerarPng(IReadOnlyList<(string Nome, decimal Saldo)> contas, decimal total, DateTime data)
        {
            using var normal = CriarFonte(SKFontStyle.Normal);
            using var negrito = CriarFonte(SKFontStyle.Bold);

            var cabecalho = ("Como Estamos", DataCabecalho(data));
            var linhas = contas.Select(c => (c.Nome, Valor(c.Saldo))).ToList();
            var rodape = ("Total", Valor(total));

            // Larguras das colunas: o maior texto de cada uma, com um mínimo parecido com a planilha.
            var larguraNome = Math.Max(LarguraMinimaNome, new[] { negrito.MeasureText(cabecalho.Item1), negrito.MeasureText(rodape.Item1) }
                .Concat(linhas.Select(l => normal.MeasureText(l.Nome))).Max() + 2 * Margem);
            var larguraValor = Math.Max(LarguraMinimaValor, new[] { negrito.MeasureText(cabecalho.Item2), negrito.MeasureText(rodape.Item2) }
                .Concat(linhas.Select(l => normal.MeasureText(l.Item2))).Max() + 2 * Margem + RespiroColunas);

            var largura = larguraNome + larguraValor;
            var altura = AlturaLinha * (linhas.Count + 2);

            var info = new SKImageInfo((int)Math.Ceiling((largura + 1) * Escala), (int)Math.Ceiling((altura + 1) * Escala));
            using var superficie = SKSurface.Create(info);
            var canvas = superficie.Canvas;
            canvas.Clear(SKColors.White);
            canvas.Scale(Escala);
            canvas.Translate(0.5f, 0.5f);

            using var pincelFundo = new SKPaint { Color = Verde, IsAntialias = false };
            using var pincelBorda = new SKPaint { Color = Borda, Style = SKPaintStyle.Stroke, StrokeWidth = 1f / Escala * 2, IsAntialias = true };
            using var pincelTexto = new SKPaint { Color = Texto, IsAntialias = true };

            var y = 0f;
            DesenharLinha(cabecalho.Item1, cabecalho.Item2, negrito, verde: true);
            foreach (var (nome, valor) in linhas)
                DesenharLinha(nome, valor, normal, verde: false);
            DesenharLinha(rodape.Item1, rodape.Item2, negrito, verde: true);

            // Grade: contorno, divisão das colunas e linhas entre as células.
            canvas.DrawRect(0, 0, largura, altura, pincelBorda);
            canvas.DrawLine(larguraNome, 0, larguraNome, altura, pincelBorda);
            for (var i = 1; i < linhas.Count + 2; i++)
                canvas.DrawLine(0, i * AlturaLinha, largura, i * AlturaLinha, pincelBorda);

            using var imagem = superficie.Snapshot();
            using var png = imagem.Encode(SKEncodedImageFormat.Png, 100);
            return png.ToArray();

            void DesenharLinha(string nome, string valor, SKFont fonte, bool verde)
            {
                if (verde)
                    canvas.DrawRect(0, y, largura, AlturaLinha, pincelFundo);

                // Texto centralizado na vertical da célula.
                fonte.GetFontMetrics(out var metricas);
                var baseTexto = y + (AlturaLinha - metricas.Descent - metricas.Ascent) / 2;
                canvas.DrawText(nome, Margem, baseTexto, SKTextAlign.Left, fonte, pincelTexto);
                canvas.DrawText(valor, largura - Margem, baseTexto, SKTextAlign.Right, fonte, pincelTexto);
                y += AlturaLinha;
            }
        }

        private static SKFont CriarFonte(SKFontStyle estilo) =>
            new(SKTypeface.FromFamilyName("Calibri", estilo) ?? SKTypeface.Default, TamanhoFonte) { Subpixel = true };
    }
}
