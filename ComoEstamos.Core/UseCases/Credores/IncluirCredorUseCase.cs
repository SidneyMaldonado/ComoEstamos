using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Credores
{
    public interface IIncluirCredorUseCase
    {
        /// <summary>Valida e grava um credor novo, ativo, do usuário informado.</summary>
        Task<Credor> ExecutarAsync(Credor credor, int idUsuario);
    }

    public class IncluirCredorUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IIncluirCredorUseCase
    {
        public async Task<Credor> ExecutarAsync(Credor credor, int idUsuario)
        {
            credor.Id = 0;
            credor.IdUsuario = idUsuario;
            credor.Ativo = true;

            await using var uow = await unitOfWorkFactory.IniciarAsync();
            await CredorValidador.ValidarAsync(uow, credor);

            await uow.Credores.InserirAsync(credor);
            await uow.ConfirmarAsync();
            return credor;
        }
    }
}
