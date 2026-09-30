namespace ComoEstamos.Core
{
    /// <summary>
    /// Operação recusada por uma regra de negócio. A mensagem é escrita para
    /// ser mostrada diretamente ao usuário.
    /// </summary>
    public class RegraDeNegocioException(string mensagem) : Exception(mensagem);
}
