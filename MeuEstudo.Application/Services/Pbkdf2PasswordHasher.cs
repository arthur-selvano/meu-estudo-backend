using System.Globalization;
using System.Security.Cryptography;
using MeuEstudo.Application.Interfaces;

namespace MeuEstudo.Application.Services;

/// <summary>
/// Implementação de hashing de senha com PBKDF2-HMAC-SHA256 (nativo do .NET).
/// O hash armazenado inclui o algoritmo, as iterações, o salt e a chave, no formato:
/// <c>pbkdf2-sha256$iteracoes$saltBase64$chaveBase64</c>.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Prefixo = "pbkdf2-sha256";
    private const int TamanhoSalt = 16;
    private const int TamanhoChave = 32;
    private const int IteracoesPadrao = 100_000;
    private const int IteracoesMaximas = 1_000_000;

    public string Hash(string senha)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(senha);

        var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var chave = Rfc2898DeriveBytes.Pbkdf2(
            senha,
            salt,
            IteracoesPadrao,
            HashAlgorithmName.SHA256,
            TamanhoChave);

        return string.Join(
            '$',
            Prefixo,
            IteracoesPadrao.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(chave));
    }

    public bool Verify(string senha, string hash)
    {
        if (string.IsNullOrWhiteSpace(senha) || string.IsNullOrWhiteSpace(hash))
            return false;

        var partes = hash.Split('$');

        if (partes.Length != 4 || partes[0] != Prefixo)
            return false;

        if (!int.TryParse(partes[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iteracoes)
            || iteracoes <= 0
            || iteracoes > IteracoesMaximas)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(partes[2]);
            var chaveEsperada = Convert.FromBase64String(partes[3]);

            if (salt.Length == 0 || chaveEsperada.Length == 0)
                return false;

            var chaveCalculada = Rfc2898DeriveBytes.Pbkdf2(
                senha,
                salt,
                iteracoes,
                HashAlgorithmName.SHA256,
                chaveEsperada.Length);

            return CryptographicOperations.FixedTimeEquals(chaveCalculada, chaveEsperada);
        }
        catch (FormatException)
        {
            // Hash malformado no armazenamento: trata como credencial inválida.
            return false;
        }
    }
}
