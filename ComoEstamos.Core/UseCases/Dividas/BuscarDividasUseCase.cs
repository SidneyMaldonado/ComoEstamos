using System.Globalization;
using ComoEstamos.Core.Formatacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Dividas
{
    /// <summary>
    /// Filtros da consulta de dívidas. <paramref name="Nome"/>: parte do nome, sem diferenciar
    /// maiúsculas nem acentos (vazio não filtra). <paramref name="MesVencimento"/>: só as dívidas com
    /// alguma parcela vencendo nesse mês (o dia é ignorado; nulo não filtra).
    /// </summary>
    public record FiltroDividas(string? Nome = null, DateTime? MesVencimento = null, bool OcultarPagas = false);

    /// <summary>Uma linha da consulta de dívidas.</summary>
    /// <param name="ValorParcela">Valor da próxima parcela em aberto (sem parcela em aberto: da primeira; sem parcelas: total ÷ número de parcelas).</param>
    /// <param name="ProximoVencimento">Vencimento da primeira parcela em aberto; nulo se todas estão pagas.</param>
    /// <param name="Paga">Todas as parcelas ativas estão pagas.</param>
    public record ResumoDivida(
        int Id,
        string Nome,
        int DiaVencimento,
        int NumeroParcelas,
        decimal ValorTotal,
        decimal ValorParcela,
        int ParcelasPagas,
        DateTime? ProximoVencimento,
        bool Paga);

    public interface IBuscarDividasUseCase
    {
        /// <summary>
        /// Dívidas ativas do usuário (não inclui créditos), filtradas. Ordem: as em aberto pelo próximo
        /// vencimento; depois as pagas, pelo último vencimento. Empate: por nome.
        /// </summary>
        Task<List<ResumoDivida>> ExecutarAsync(FiltroDividas filtro, int idUsuario);
    }

    public class BuscarDividasUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IBuscarDividasUseCase
    {
        public async Task<List<ResumoDivida>> ExecutarAsync(FiltroDividas filtro, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();
            var dividas = await uow.Dividas.ListarPorUsuarioAsync(idUsuario);
            var parcelas = (await uow.Parcelas.ListarPorUsuarioAsync(idUsuario)).ToLookup(p => p.IdDivida);

            var nome = filtro.Nome?.Trim();
            var linhas =
                from divida in dividas
                where divida.EhDivida
                where string.IsNullOrEmpty(nome) || ContemNome(divida.Nome, nome)
                let parcelasDaDivida = parcelas[divida.Id].ToList()
                where filtro.MesVencimento is not DateTime mes || parcelasDaDivida.Any(p => MesmoMes(p.DataVencimento, mes))
                let resumo = Resumir(divida, parcelasDaDivida)
                where !(filtro.OcultarPagas && resumo.Paga)
                select (resumo, ultimoVencimento: parcelasDaDivida.Select(p => (DateTime?)p.DataVencimento).Max());

            return linhas
                .OrderBy(l => l.resumo.Paga)
                .ThenBy(l => l.resumo.ProximoVencimento ?? l.ultimoVencimento ?? DateTime.MaxValue)
                .ThenBy(l => l.resumo.Nome, StringComparer.Create(FormatoBr.Cultura, ignoreCase: true))
                .Select(l => l.resumo)
                .ToList();
        }

        private static ResumoDivida Resumir(Divida divida, List<Parcela> parcelas)
        {
            var emAberto = parcelas.Where(p => p.DataPagamento is null).ToList();
            // Depois de uma alteração com parcelas pagas, as em aberto podem ter outro valor que as pagas.
            var referencia = emAberto.FirstOrDefault() ?? parcelas.FirstOrDefault();
            return new ResumoDivida(
                divida.Id,
                divida.Nome,
                divida.DiaVencimento,
                divida.NumeroParcelas,
                divida.Valor,
                referencia?.Valor ?? Parcelamento.ValorDaParcela(divida.Valor, divida.NumeroParcelas),
                parcelas.Count - emAberto.Count,
                emAberto.Select(p => (DateTime?)p.DataVencimento).Min(),
                Paga: parcelas.Count > 0 && emAberto.Count == 0);
        }

        private static bool ContemNome(string nome, string parte) =>
            FormatoBr.Cultura.CompareInfo.IndexOf(nome, parte, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

        private static bool MesmoMes(DateTime data, DateTime mes) => data.Year == mes.Year && data.Month == mes.Month;
    }
}
