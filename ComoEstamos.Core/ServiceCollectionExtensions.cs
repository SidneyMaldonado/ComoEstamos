using ComoEstamos.Core.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace ComoEstamos.Core
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>Registra os casos de uso. Requer <c>AddComoEstamosData</c>.</summary>
        public static IServiceCollection AddComoEstamosCore(this IServiceCollection services)
        {
            services.AddSingleton<IPagarParcelaUseCase, PagarParcelaUseCase>();
            services.AddSingleton<ICriarDividaUseCase, CriarDividaUseCase>();
            services.AddSingleton<IRegistrarOperacaoUseCase, RegistrarOperacaoUseCase>();

            return services;
        }
    }
}
