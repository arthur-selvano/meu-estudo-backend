namespace MeuEstudo.Api.Services;

/// <summary>Gera os tokens JWT usados na autenticação da API.</summary>
public interface ITokenService
{
    /// <summary>
    /// Emite um token assinado (HS256) para o usuário. O token contém apenas o
    /// identificador e o nome; dados sensíveis como e-mail e hash de senha não entram no JWT.
    /// </summary>
    string GerarToken(Guid usuarioId, string nome);
}
