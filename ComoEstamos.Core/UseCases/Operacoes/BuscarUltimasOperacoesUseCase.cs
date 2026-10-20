using ComoEstamos.Core.UseCases.Investimentos;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Operacoes
{
    public interface IBuscarUltimasOperacoesUseCase
    {
        /// <summary>
        /// As <paramref name="quantidade"/> operações ativas mais recentes do investimento, da mais recente para a
        /// mais antiga. Lista vazia se o investimento não existe, é de outro usuário ou foi excluído.
        /// </summary>
        Task<List<Operacao>> ExecutarAsync(int idInvestimento, int quantidade, int idUsuario);
    }

    public class BuscarUltimasOperacoesUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IBuscarUltimasOperacoesUseCase
    {
        public async Task<List<Operacao>> ExecutarAsync(int idInvestimento, int quantidade, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();
            var investimento = await InvestimentoValidador.ObterDoUsuarioAsync(uow, idInvestimento, idUsuario);
            if (investimento is not { Ativo: true })
                return [];
            return await uow.Operacoes.ListarUltimasPorInvestimentoAsync(idInvestimento, quantidade);
        }
    }
}
