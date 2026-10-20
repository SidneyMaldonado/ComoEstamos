using ComoEstamos.Core.Formatacao;

namespace ComoEstamos.Controls
{
    /// <summary>
    /// Não deixa digitar mais de <see cref="Casas"/> casas decimais no campo (padrão 2, para valores em dinheiro):
    /// a tecla que passaria do limite é desfeita. A leitura é a do <see cref="FormatoBr.TentarLerDecimal"/>, então
    /// o ponto de milhar (<c>1.250</c>) continua aceito. O caso de uso valida as casas de novo ao salvar.
    /// </summary>
    public class LimiteCasasDecimais : Behavior<Entry>
    {
        public int Casas { get; set; } = 2;

        protected override void OnAttachedTo(Entry campo)
        {
            base.OnAttachedTo(campo);
            campo.TextChanged += Campo_TextChanged;
        }

        protected override void OnDetachingFrom(Entry campo)
        {
            campo.TextChanged -= Campo_TextChanged;
            base.OnDetachingFrom(campo);
        }

        private void Campo_TextChanged(object? sender, TextChangedEventArgs e)
        {
            // Se o texto anterior também passava (veio do código), limpa: voltar para ele dispararia o evento sem fim.
            if (sender is Entry campo && FormatoBr.PassaDasCasas(e.NewTextValue, Casas))
                campo.Text = FormatoBr.PassaDasCasas(e.OldTextValue, Casas) ? string.Empty : e.OldTextValue;
        }
    }
}
