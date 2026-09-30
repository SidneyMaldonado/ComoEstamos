using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases
{
    public interface ICriarDividaUseCase
    {
        /// <summary>
        /// Cadastra a dívida (ou crédito) e gera suas parcelas. <see cref="Divida.Valor"/>
        /// é o valor total, dividido igualmente entre as parcelas.
        /// </summary>
        /// <returns>A dívida gravada, com <see cref="Divida.Parcelas"/> preenchida.</returns>
        Task<Divida> ExecutarAsync(Divida divida);
    }

    public class CriarDividaUseCase(IUnitOfWorkFactory unitOfWorkFactory) : ICriarDividaUseCase
    {
        private const int TamanhoMaximoDescricao = 100;

        public async Task<Divida> ExecutarAsync(Divida divida)
        {
            Validar(divida);

            await using var uow = await unitOfWorkFactory.IniciarAsync();

            var usuario = await uow.Usuarios.ObterPorIdAsync(divida.IdUsuario);
            if (usuario is not { Ativo: true })
                throw new RegraDeNegocioException("Usuário não encontrado ou desativado.");

            var conta = await uow.Contas.ObterPorIdAsync(divida.IdConta!.Value);
            if (conta is not { Ativo: true } || conta.IdUsuario != divida.IdUsuario)
                throw new RegraDeNegocioException("Conta não encontrada ou desativada.");

            var categoria = await uow.Categorias.ObterPorIdAsync(divida.IdCategoria!.Value);
            if (categoria is not { Ativo: true } || categoria.IdUsuario != divida.IdUsuario)
                throw new RegraDeNegocioException("Categoria não encontrada ou desativada.");

            if (divida.IdCredor is int idCredor)
            {
                var credor = await uow.Credores.ObterPorIdAsync(idCredor);
                if (credor is not { Ativo: true } || credor.IdUsuario != divida.IdUsuario)
                    throw new RegraDeNegocioException("Credor não encontrado ou desativado.");
            }

            // As parcelas são sempre geradas aqui; o que vier preenchido é descartado.
            divida.Parcelas.Clear();
            divida.DataPrimeiroVencimento = divida.DataPrimeiroVencimento.Date;
            await uow.Dividas.InserirAsync(divida);

            // A dívida ainda não tem Id: as parcelas se ligam a ela pela navegação,
            // e o EF preenche IdDivida e Divida.Parcelas ao gravar.
            foreach (var parcela in GerarParcelas(divida))
                await uow.Parcelas.InserirAsync(parcela);

            await uow.ConfirmarAsync();
            return divida;
        }

        private static void Validar(Divida divida)
        {
            if (string.IsNullOrWhiteSpace(divida.Nome))
                throw new RegraDeNegocioException("Informe o nome.");
            if (divida.DiaVencimento is < 1 or > 31)
                throw new RegraDeNegocioException("O dia de vencimento deve estar entre 1 e 31.");
            if (divida.NumeroParcelas < 1)
                throw new RegraDeNegocioException("O número de parcelas deve ser pelo menos 1.");
            if (divida.Valor <= 0)
                throw new RegraDeNegocioException("O valor deve ser maior que zero.");
            if (divida.IdConta is null)
                throw new RegraDeNegocioException("Informe a conta.");
            if (divida.IdCategoria is null)
                throw new RegraDeNegocioException("Informe a categoria.");
        }

        /// <summary>
        /// A 1ª parcela vence em <see cref="Divida.DataPrimeiroVencimento"/>; as seguintes,
        /// nos meses subsequentes, no <see cref="Divida.DiaVencimento"/> (ou no último dia
        /// do mês, se ele for menor). O valor é dividido igualmente, com os centavos
        /// que sobrarem do arredondamento na última parcela.
        /// </summary>
        private static List<Parcela> GerarParcelas(Divida divida)
        {
            var total = divida.NumeroParcelas;
            var valorParcela = Math.Round(divida.Valor / total, 2, MidpointRounding.ToZero);
            var inicio = divida.DataPrimeiroVencimento.Date;
            var parcelas = new List<Parcela>(total);

            for (var i = 0; i < total; i++)
            {
                var mes = inicio.AddMonths(i);
                var vencimento = i == 0
                    ? inicio
                    : new DateTime(mes.Year, mes.Month, Math.Min(divida.DiaVencimento, DateTime.DaysInMonth(mes.Year, mes.Month)));

                parcelas.Add(new Parcela
                {
                    Divida = divida,
                    IdConta = divida.IdConta!.Value,
                    IdCategoria = divida.IdCategoria!.Value,
                    Descricao = MontarDescricao(divida.Nome, i + 1, total),
                    Valor = i == total - 1 ? divida.Valor - valorParcela * (total - 1) : valorParcela,
                    DataVencimento = vencimento
                });
            }

            return parcelas;
        }

        private static string MontarDescricao(string nome, int numero, int total)
        {
            var sufixo = $" {numero}/{total}";
            var nomeCortado = nome.Trim();
            if (nomeCortado.Length + sufixo.Length > TamanhoMaximoDescricao)
                nomeCortado = nomeCortado[..(TamanhoMaximoDescricao - sufixo.Length)];
            return nomeCortado + sufixo;
        }
    }
}
