using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Contas
{
    public interface IIncluirContaUseCase
    {
        /// <summary>Valida e grava uma conta nova, ativa, do usuário informado.</summary>
        Task<Conta> ExecutarAsync(Conta conta, int idUsuario);
    }

    public class IncluirContaUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IIncluirContaUseCase
    {
        public async Task<Conta> ExecutarAsync(Conta conta, int idUsuario)
        {
            conta.Id = 0;
            conta.IdUsuario = idUsuario;
            conta.Ativo = true;

            await using var uow = await unitOfWorkFactory.IniciarAsync();
            await ContaValidador.ValidarAsync(uow, conta);

            await uow.Contas.InserirAsync(conta);
            await uow.ConfirmarAsync();
            return conta;
        }
    }
}
