namespace MeuEstudos.Api.Services;

/// <summary>Gera os tokens JWT usados na autenticação da API.</summary>
public interface ITokenService
{
    string GerarToken(Guid usuarioId, string nome, string email);
}
