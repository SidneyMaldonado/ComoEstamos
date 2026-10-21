namespace ComoEstamos.Controls
{
    /// <summary>
    /// <see cref="ScrollView"/> que serve de atalho de navegação: avisa quando o dedo arrasta para cima com a rolagem
    /// já no fim, ou para baixo com ela já no topo (o arrasto que a rolagem não usa). Em cima das linhas, onde um
    /// <see cref="SwipeGestureRecognizer"/> não dispara porque a rolagem consome o arrasto. Só no Android
    /// (<c>Platforms/Android/RolagemComAtalhosHandler</c>); nas outras plataformas é uma <see cref="ScrollView"/> comum.
    /// </summary>
    public class RolagemComAtalhos : ScrollView
    {
        /// <summary>O dedo arrastou para cima com a rolagem no fim (ou sem nada para rolar).</summary>
        public event EventHandler? ArrastouParaCimaNoFim;

        /// <summary>O dedo arrastou para baixo com a rolagem no topo (ou sem nada para rolar).</summary>
        public event EventHandler? ArrastouParaBaixoNoInicio;

        internal void AvisarArrastoParaCima() => ArrastouParaCimaNoFim?.Invoke(this, EventArgs.Empty);

        internal void AvisarArrastoParaBaixo() => ArrastouParaBaixoNoInicio?.Invoke(this, EventArgs.Empty);
    }
}
