using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Categorias
{
    public interface IIncluirCategoriaUseCase
    {
        /// <summary>Valida e grava uma categoria nova, ativa, do usuário informado.</summary>
        Task<Categoria> ExecutarAsync(Categoria categoria, int idUsuario);
    }

    public class IncluirCategoriaUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IIncluirCategoriaUseCase
    {
        public async Task<Categoria> ExecutarAsync(Categoria categoria, int idUsuario)
        {
            categoria.Id = 0;
            categoria.IdUsuario = idUsuario;
            categoria.Ativo = true;

            await using var uow = await unitOfWorkFactory.IniciarAsync();
            await CategoriaValidador.ValidarAsync(uow, categoria);

            await uow.Categorias.InserirAsync(categoria);
            await uow.ConfirmarAsync();
            return categoria;
        }
    }
}
