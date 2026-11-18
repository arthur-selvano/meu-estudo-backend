using MeuEstudos.Domain.Repositories;
using MeuEstudos.Infrastructure.Data;
using MeuEstudos.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MeuEstudos.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra o contexto do EF Core e as implementações dos repositórios do domínio.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="config">Configuração onde a connection string "DefaultConnection" é lida.</param>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "A connection string 'DefaultConnection' não foi configurada.");

        services.AddDbContext<AppDbContext>(options =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IGrupoEstudoRepository, GrupoEstudoRepository>();

        return services;
    }
}
