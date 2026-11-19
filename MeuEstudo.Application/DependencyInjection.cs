using MeuEstudo.Application.Interfaces;
using MeuEstudo.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MeuEstudo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGrupoEstudoService, GrupoEstudoService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IPublicacaoService, PublicacaoService>();

        // Sem estado e thread-safe: não há necessidade de criar uma instância por escopo.
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        return services;
    }
}
