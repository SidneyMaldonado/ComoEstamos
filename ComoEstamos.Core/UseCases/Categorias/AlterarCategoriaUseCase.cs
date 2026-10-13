using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Categorias
{
    public interface IAlterarCategoriaUseCase
    {
        /// <summary>Valida e grava o nome da categoria. Os demais campos gravados são mantidos.</summary>
        Task ExecutarAsync(Categoria categoria, int idUsuario);
    }

    public class AlterarCategoriaUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IAlterarCategoriaUseCase
    {
        public async Task ExecutarAsync(Categoria categoria, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            var gravada = await CategoriaValidador.ObterAtivaDoUsuarioAsync(uow, categoria.Id, idUsuario);
            categoria.IdUsuario = idUsuario;
            await CategoriaValidador.ValidarAsync(uow, categoria);

            gravada.Nome = categoria.Nome;
            await uow.ConfirmarAsync();
        }
    }
}
