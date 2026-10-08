namespace ComoEstamos.Core.Validacao
{
    /// <summary>
    /// Um ou mais campos fora do domínio. Herda de <see cref="RegraDeNegocioException"/>:
    /// quem não trata campo a campo pode mostrar a mensagem, que junta todos os erros.
    /// </summary>
    public class ValidacaoException(IReadOnlyList<ErroValidacao> erros)
        : RegraDeNegocioException(string.Join(Environment.NewLine, erros.Select(e => e.Mensagem)))
    {
        public IReadOnlyList<ErroValidacao> Erros { get; } = erros;
    }
}
