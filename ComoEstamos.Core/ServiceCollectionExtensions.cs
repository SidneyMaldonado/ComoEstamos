using ComoEstamos.Core.Services;
using ComoEstamos.Core.UseCases;
using ComoEstamos.Core.UseCases.Carteiras;
using ComoEstamos.Core.UseCases.Categorias;
using ComoEstamos.Core.UseCases.Contas;
using ComoEstamos.Core.UseCases.Credores;
using ComoEstamos.Core.UseCases.Investimentos;
using Microsoft.Extensions.DependencyInjection;

namespace ComoEstamos.Core
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>Registra os casos de uso, os services e o usuário atual. Requer <c>AddComoEstamosData</c>.</summary>
        public static IServiceCollection AddComoEstamosCore(this IServiceCollection services)
        {
            services.AddSingleton<IPagarParcelaUseCase, PagarParcelaUseCase>();
            services.AddSingleton<ICriarDividaUseCase, CriarDividaUseCase>();
            services.AddSingleton<IRegistrarOperacaoUseCase, RegistrarOperacaoUseCase>();

            services.AddSingleton<IUsuarioAtual, UsuarioAtual>();

            // CRUDs: os 5 casos de uso e o service de cada entidade (Spec/07-Crud.md).
            services.AddSingleton<IBuscarTodasContasUseCase, BuscarTodasContasUseCase>();
            services.AddSingleton<IBuscarContaPorIdUseCase, BuscarContaPorIdUseCase>();
            services.AddSingleton<IIncluirContaUseCase, IncluirContaUseCase>();
            services.AddSingleton<IAlterarContaUseCase, AlterarContaUseCase>();
            services.AddSingleton<IExcluirContaUseCase, ExcluirContaUseCase>();
            services.AddSingleton<IContaService, ContaService>();

            services.AddSingleton<IBuscarTodasCarteirasUseCase, BuscarTodasCarteirasUseCase>();
            services.AddSingleton<IBuscarCarteiraPorIdUseCase, BuscarCarteiraPorIdUseCase>();
            services.AddSingleton<IIncluirCarteiraUseCase, IncluirCarteiraUseCase>();
            services.AddSingleton<IAlterarCarteiraUseCase, AlterarCarteiraUseCase>();
            services.AddSingleton<IExcluirCarteiraUseCase, ExcluirCarteiraUseCase>();
            services.AddSingleton<ICarteiraService, CarteiraService>();

            services.AddSingleton<IBuscarTodasCategoriasUseCase, BuscarTodasCategoriasUseCase>();
            services.AddSingleton<IBuscarCategoriaPorIdUseCase, BuscarCategoriaPorIdUseCase>();
            services.AddSingleton<IIncluirCategoriaUseCase, IncluirCategoriaUseCase>();
            services.AddSingleton<IAlterarCategoriaUseCase, AlterarCategoriaUseCase>();
            services.AddSingleton<IExcluirCategoriaUseCase, ExcluirCategoriaUseCase>();
            services.AddSingleton<ICategoriaService, CategoriaService>();

            services.AddSingleton<IBuscarTodosCredoresUseCase, BuscarTodosCredoresUseCase>();
            services.AddSingleton<IBuscarCredorPorIdUseCase, BuscarCredorPorIdUseCase>();
            services.AddSingleton<IIncluirCredorUseCase, IncluirCredorUseCase>();
            services.AddSingleton<IAlterarCredorUseCase, AlterarCredorUseCase>();
            services.AddSingleton<IExcluirCredorUseCase, ExcluirCredorUseCase>();
            services.AddSingleton<ICredorService, CredorService>();

            services.AddSingleton<IBuscarTodosInvestimentosUseCase, BuscarTodosInvestimentosUseCase>();
            services.AddSingleton<IBuscarInvestimentoPorIdUseCase, BuscarInvestimentoPorIdUseCase>();
            services.AddSingleton<IIncluirInvestimentoUseCase, IncluirInvestimentoUseCase>();
            services.AddSingleton<IAlterarInvestimentoUseCase, AlterarInvestimentoUseCase>();
            services.AddSingleton<IExcluirInvestimentoUseCase, ExcluirInvestimentoUseCase>();
            services.AddSingleton<IInvestimentoService, InvestimentoService>();

            return services;
        }
    }
}
