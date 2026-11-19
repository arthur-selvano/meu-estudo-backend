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

    // Recomendação OWASP (2023+) para PBKDF2-HMAC-SHA256. O custo fica no
    // servidor; hashes antigos (100k) continuam válidos e são atualizados no login.
    private const int IteracoesPadrao = 600_000;
    private const int IteracoesMaximas = 2_000_000;

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

        if (!TentarLerHash(hash, out var iteracoes, out var salt, out var chaveEsperada))
            return false;

        var chaveCalculada = Rfc2898DeriveBytes.Pbkdf2(
            senha,
            salt,
            iteracoes,
            HashAlgorithmName.SHA256,
            chaveEsperada.Length);

        return CryptographicOperations.FixedTimeEquals(chaveCalculada, chaveEsperada);
    }

    public bool PrecisaRehash(string hash)
    {
        if (!TentarLerHash(hash, out var iteracoes, out _, out var chave))
            return true;

        return iteracoes < IteracoesPadrao || chave.Length < TamanhoChave;
    }

    private static bool TentarLerHash(
        string hash,
        out int iteracoes,
        out byte[] salt,
        out byte[] chave)
    {
        iteracoes = 0;
        salt = [];
        chave = [];

        var partes = hash.Split('$');

        if (partes.Length != 4 || partes[0] != Prefixo)
            return false;

        if (!int.TryParse(partes[1], NumberStyles.None, CultureInfo.InvariantCulture, out iteracoes)
            || iteracoes <= 0
            || iteracoes > IteracoesMaximas)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(partes[2]);
            chave = Convert.FromBase64String(partes[3]);

            return salt.Length > 0 && chave.Length > 0;
        }
        catch (FormatException)
        {
            // Hash malformado no armazenamento: trata como credencial inválida.
            return false;
        }
    }
}
