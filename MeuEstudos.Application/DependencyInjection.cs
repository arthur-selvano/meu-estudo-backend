using MeuEstudos.Application.Interfaces;
using MeuEstudos.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MeuEstudos.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGrupoEstudoService, GrupoEstudoService>();

        // Sem estado e thread-safe: não há necessidade de criar uma instância por escopo.
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        return services;
    }
}
