using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Credores
{
    public interface IAlterarCredorUseCase
    {
        /// <summary>Valida e grava o nome e as observações do credor. Os demais campos gravados são mantidos.</summary>
        Task ExecutarAsync(Credor credor, int idUsuario);
    }

    public class AlterarCredorUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IAlterarCredorUseCase
    {
        public async Task ExecutarAsync(Credor credor, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            var gravado = await CredorValidador.ObterAtivoDoUsuarioAsync(uow, credor.Id, idUsuario);
            credor.IdUsuario = idUsuario;
            await CredorValidador.ValidarAsync(uow, credor);

            gravado.Nome = credor.Nome;
            gravado.Observacoes = credor.Observacoes;
            await uow.ConfirmarAsync();
        }
    }
}
