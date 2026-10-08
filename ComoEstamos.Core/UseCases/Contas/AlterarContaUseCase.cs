using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Contas
{
    public interface IAlterarContaUseCase
    {
        /// <summary>Valida e grava o nome e o saldo da conta. Os demais campos gravados são mantidos.</summary>
        Task ExecutarAsync(Conta conta, int idUsuario);
    }

    public class AlterarContaUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IAlterarContaUseCase
    {
        public async Task ExecutarAsync(Conta conta, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            var gravada = await ContaValidador.ObterAtivaDoUsuarioAsync(uow, conta.Id, idUsuario);
            conta.IdUsuario = idUsuario;
            await ContaValidador.ValidarAsync(uow, conta);

            gravada.Nome = conta.Nome;
            gravada.Saldo = conta.Saldo;
            await uow.ConfirmarAsync();
        }
    }
}
