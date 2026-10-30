using System.Reflection;

namespace Chamapro.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServicesERepositories(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var tipos = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => (t.Namespace?.StartsWith("Chamapro.Api.Services") ?? false) && t.Name.EndsWith("Service")
                     || (t.Namespace?.StartsWith("Chamapro.Api.Repositories") ?? false) && t.Name.EndsWith("Repository"));

        foreach (var implementacao in tipos)
        {
            var interfaceEsperada = "I" + implementacao.Name;
            var contrato = implementacao.GetInterfaces().FirstOrDefault(i => i.Name == interfaceEsperada);
            if (contrato is not null)
                services.AddScoped(contrato, implementacao);
        }

        var rotinas = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(BackgroundService)));

        foreach (var rotina in rotinas)
            services.AddSingleton(typeof(IHostedService), rotina);

        return services;
    }
}
