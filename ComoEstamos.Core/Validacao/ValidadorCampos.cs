using ComoEstamos.Core.Formatacao;

namespace ComoEstamos.Core.Validacao
{
    /// <summary>
    /// Acumula os erros de validação de uma entidade, no máximo um por campo (o primeiro).
    /// Ao final, <see cref="LancarSeHouverErros"/> lança todos juntos numa <see cref="ValidacaoException"/>.
    /// </summary>
    /// <remarks>
    /// O <c>rotulo</c> dos métodos é o nome do campo com artigo e em minúsculas ("o nome da conta"),
    /// usado para montar mensagens como "Informe o nome da conta.".
    /// </remarks>
    public sealed class ValidadorCampos
    {
        private readonly List<ErroValidacao> erros = [];

        public IReadOnlyList<ErroValidacao> Erros => erros;

        public bool TemErro(string campo) => erros.Any(e => e.Campo == campo);

        /// <summary>Registra um erro, a menos que o campo já tenha um.</summary>
        public void Adicionar(string campo, string mensagem)
        {
            if (!TemErro(campo))
                erros.Add(new ErroValidacao(campo, mensagem));
        }

        /// <summary>
        /// Texto já normalizado (sem espaços nas pontas): obrigatoriedade e tamanho máximo
        /// (<c>null</c> quando a coluna não tem limite).
        /// </summary>
        public void Texto(string campo, string rotulo, string? valor, int? tamanhoMaximo, bool obrigatorio)
        {
            if (string.IsNullOrEmpty(valor))
            {
                if (obrigatorio)
                    Adicionar(campo, $"Informe {rotulo}.");
            }
            else if (valor.Length > tamanhoMaximo)
            {
                Adicionar(campo, $"{Maiuscula(rotulo)} deve ter no máximo {tamanhoMaximo} caracteres.");
            }
        }

        /// <summary>Número decimal: quantidade de casas decimais e faixa permitida.</summary>
        public void Numero(string campo, string rotulo, decimal valor, int casasDecimais, decimal minimo, decimal maximo)
        {
            if (decimal.Round(valor, casasDecimais) != valor)
                Adicionar(campo, $"{Maiuscula(rotulo)} deve ter no máximo {casasDecimais} casas decimais.");
            else if (valor < minimo || valor > maximo)
                Adicionar(campo, $"{Maiuscula(rotulo)} deve estar entre " +
                    $"{FormatoBr.Numero(minimo, casasDecimais)} e {FormatoBr.Numero(maximo, casasDecimais)}.");
        }

        public void LancarSeHouverErros()
        {
            if (erros.Count > 0)
                throw new ValidacaoException(erros);
        }

        /// <summary>Remove os espaços nas pontas; <c>null</c> vira texto vazio.</summary>
        public static string Normalizar(string? texto) => texto?.Trim() ?? string.Empty;

        /// <summary>Remove os espaços nas pontas; texto vazio vira <c>null</c> (campos opcionais).</summary>
        public static string? NormalizarOpcional(string? texto) =>
            string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

        /// <summary>Compara nomes como o usuário enxerga: sem diferenciar maiúsculas e minúsculas.</summary>
        public static bool MesmoNome(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

        private static string Maiuscula(string texto) => char.ToUpper(texto[0], FormatoBr.Cultura) + texto[1..];
    }
}
