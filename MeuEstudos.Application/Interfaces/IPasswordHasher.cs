namespace MeuEstudos.Application.Interfaces;

/// <summary>
/// Gera e verifica hashes de senha. A senha em texto puro nunca deve ser persistida.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Gera um hash seguro (com salt aleatório) para a senha informada.</summary>
    string Hash(string senha);

    /// <summary>Verifica se a senha corresponde ao hash armazenado.</summary>
    bool Verify(string senha, string hash);
}
