using ComoEstamos.Core.UseCases.Operacoes;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.Services
{
    /// <summary>Operações (compra e venda) de investimento usadas pelas telas, sempre nas carteiras do usuário atual.</summary>
    public interface IOperacaoService
    {
        /// <summary>As <paramref name="quantidade"/> operações mais recentes do investimento.</summary>
        Task<List<Operacao>> BuscarUltimasAsync(int idInvestimento, int quantidade);

        /// <summary>Registra a compra ou venda; a quantidade do investimento muda junto.</summary>
        Task<Operacao> RegistrarAsync(Operacao operacao);

        /// <summary>Exclui a operação e desfaz o efeito dela na quantidade do investimento.</summary>
        Task ExcluirAsync(int id);
    }

    public class OperacaoService(
        IUsuarioAtual usuarioAtual,
        IBuscarUltimasOperacoesUseCase buscarUltimas,
        IRegistrarOperacaoUseCase registrar,
        IExcluirOperacaoUseCase excluir) : IOperacaoService
    {
        public async Task<List<Operacao>> BuscarUltimasAsync(int idInvestimento, int quantidade) =>
            await buscarUltimas.ExecutarAsync(idInvestimento, quantidade, await usuarioAtual.ObterIdAsync());

        public async Task<Operacao> RegistrarAsync(Operacao operacao) =>
            await registrar.ExecutarAsync(operacao, await usuarioAtual.ObterIdAsync());

        public async Task ExcluirAsync(int id) =>
            await excluir.ExecutarAsync(id, await usuarioAtual.ObterIdAsync());
    }
}
