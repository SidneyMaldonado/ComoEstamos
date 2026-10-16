using ComoEstamos.Core.Formatacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Dividas
{
    public interface IAlterarDividaUseCase
    {
        /// <summary>
        /// Valida e grava nome, credor, conta, categoria, valor total, número de parcelas, dia e primeiro
        /// vencimento. As parcelas pagas não mudam; as em aberto recebem a nova conta e, se um
        /// campo financeiro mudou, são refeitas com o que falta pagar.
        /// </summary>
        Task ExecutarAsync(Divida divida, int idUsuario);
    }

    public class AlterarDividaUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IAlterarDividaUseCase
    {
        public async Task ExecutarAsync(Divida divida, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            var gravada = await DividaValidador.ObterAtivaDoUsuarioAsync(uow, divida.Id, idUsuario);
            // O tipo (dívida ou receita) não muda; vale o gravado, também para as mensagens.
            divida.EhDivida = gravada.EhDivida;
            var validador = await DividaValidador.ValidarCamposAsync(uow, divida, idUsuario);

            var parcelas = await DividaValidador.ObterParcelasAtivasAsync(uow, gravada.Id);
            var pagas = parcelas.Where(p => p.DataPagamento is not null).OrderBy(p => p.DataVencimento).ToList();
            var emAberto = parcelas.Where(p => p.DataPagamento is null).ToList();
            var valorPago = pagas.Sum(p => p.Valor);
            var termoPagas = DividaValidador.Pagas(gravada);

            var financeiroMudou = divida.Valor != gravada.Valor
                || divida.NumeroParcelas != gravada.NumeroParcelas
                || divida.DiaVencimento != gravada.DiaVencimento
                || divida.DataPrimeiroVencimento.Date != gravada.DataPrimeiroVencimento.Date;

            if (financeiroMudou && pagas.Count > 0)
            {
                if (divida.DataPrimeiroVencimento.Date != gravada.DataPrimeiroVencimento.Date)
                    validador.Adicionar(nameof(Divida.DataPrimeiroVencimento),
                        $"O primeiro vencimento não pode mudar: já há parcelas {termoPagas}.");

                if (!validador.TemErro(nameof(Divida.NumeroParcelas)) && divida.NumeroParcelas < pagas.Count)
                    validador.Adicionar(nameof(Divida.NumeroParcelas),
                        $"Já há {pagas.Count} parcelas {termoPagas}: o número de parcelas deve ser pelo menos {pagas.Count}.");

                if (!validador.TemErro(nameof(Divida.Valor)) && !validador.TemErro(nameof(Divida.NumeroParcelas)))
                {
                    var restantes = divida.NumeroParcelas - pagas.Count;
                    var valorRestante = divida.Valor - valorPago;
                    if (restantes == 0 && valorRestante != 0)
                        validador.Adicionar(nameof(Divida.Valor),
                            $"Todas as parcelas já estão {termoPagas}: o valor total deve ser {FormatoBr.Moeda(valorPago)}.");
                    else if (restantes > 0 && valorRestante < 0.01m * restantes)
                        validador.Adicionar(nameof(Divida.Valor),
                            $"O valor total deve ser pelo menos {FormatoBr.Moeda(valorPago + 0.01m * restantes)} " +
                            $"(já foram {(gravada.EhDivida ? "pagos" : "recebidos")} {FormatoBr.Moeda(valorPago)}).");
                }
            }

            validador.LancarSeHouverErros();

            gravada.Nome = divida.Nome;
            gravada.IdCredor = divida.IdCredor;
            gravada.IdConta = divida.IdConta;
            gravada.IdCategoria = divida.IdCategoria;
            gravada.Valor = divida.Valor;
            gravada.NumeroParcelas = divida.NumeroParcelas;
            gravada.DiaVencimento = divida.DiaVencimento;
            gravada.DataPrimeiroVencimento = divida.DataPrimeiroVencimento.Date;

            if (financeiroMudou)
            {
                // As pagas ficam com os números 1..k; as em aberto são trocadas por novas, de k+1 até o total,
                // dividindo o que falta pagar.
                foreach (var parcela in emAberto)
                    parcela.Ativo = false;
                for (var i = 0; i < pagas.Count; i++)
                    pagas[i].Descricao = Parcelamento.Descricao(gravada.Nome, i + 1, gravada.NumeroParcelas);
                foreach (var nova in Parcelamento.Gerar(gravada, pagas.Count + 1, gravada.Valor - valorPago))
                {
                    nova.IdDivida = gravada.Id;
                    await uow.Parcelas.InserirAsync(nova);
                }
            }
            else
            {
                // Mesmas parcelas: só a descrição (nome) e, nas em aberto, a conta.
                var ordenadas = parcelas.OrderBy(p => p.DataVencimento).ToList();
                for (var i = 0; i < ordenadas.Count; i++)
                    ordenadas[i].Descricao = Parcelamento.Descricao(gravada.Nome, i + 1, gravada.NumeroParcelas);
                foreach (var parcela in emAberto)
                    parcela.IdConta = gravada.IdConta!.Value;
            }

            await uow.ConfirmarAsync();
        }
    }
}
