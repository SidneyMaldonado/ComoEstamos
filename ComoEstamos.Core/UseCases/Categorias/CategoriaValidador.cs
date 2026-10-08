using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Categorias
{
    /// <summary>Regras comuns aos casos de uso de categoria.</summary>
    internal static class CategoriaValidador
    {
        public const int TamanhoMaximoNome = 100;

        /// <summary>
        /// Normaliza os campos de <paramref name="categoria"/> e valida o domínio de todos eles.
        /// Lança <see cref="ValidacaoException"/> com todos os erros encontrados.
        /// </summary>
        public static async Task ValidarAsync(IUnitOfWork uow, Categoria categoria)
        {
            categoria.Nome = ValidadorCampos.Normalizar(categoria.Nome);

            var validador = new ValidadorCampos();
            validador.Texto(nameof(Categoria.Nome), "o nome da categoria", categoria.Nome, TamanhoMaximoNome, obrigatorio: true);

            if (!validador.TemErro(nameof(Categoria.Nome)))
            {
                var existentes = await uow.Categorias.ListarPorUsuarioAsync(categoria.IdUsuario);
                if (existentes.Any(c => c.Id != categoria.Id && ValidadorCampos.MesmoNome(c.Nome, categoria.Nome)))
                    validador.Adicionar(nameof(Categoria.Nome), "Já existe uma categoria com este nome.");
            }

            validador.LancarSeHouverErros();
        }

        /// <summary>
        /// Categoria rastreada pela unidade de trabalho, para alterar ou excluir.
        /// Inexistente ou de outro usuário: <see cref="KeyNotFoundException"/>. Inativa: <see cref="RegraDeNegocioException"/>.
        /// </summary>
        public static async Task<Categoria> ObterAtivaDoUsuarioAsync(IUnitOfWork uow, int id, int idUsuario)
        {
            var categoria = await uow.Categorias.ObterPorIdAsync(id);
            if (categoria is null || categoria.IdUsuario != idUsuario)
                throw new KeyNotFoundException("A categoria não foi encontrada.");
            if (!categoria.Ativo)
                throw new RegraDeNegocioException("A categoria foi excluída.");
            return categoria;
        }
    }
}
