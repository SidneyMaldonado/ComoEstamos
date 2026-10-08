namespace ComoEstamos.Core.Validacao
{
    /// <summary>
    /// Erro de um campo. <see cref="Campo"/> é o nome da propriedade da entidade
    /// (<c>nameof(Conta.Nome)</c>), usado pela tela para mostrar a mensagem abaixo do campo certo.
    /// </summary>
    public record ErroValidacao(string Campo, string Mensagem);
}
