using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MeuEstudo.Api.Security;

/// <summary>
/// Impede que credenciais já expostas no histórico público do repositório
/// voltem a ser usadas em qualquer ambiente. Os valores são comparados por
/// hash para não reintroduzir o segredo em texto puro no código.
/// </summary>
public static partial class SegredosVazados
{
    private const string HashDaSenhaDeBancoVazada =
        "71bbaa0fb69b0b7bf58fafbe06d23d43d19fb6e1c7b3f85fd8a3d1fcb6452de6";

    public static bool UsaSenhaDeBancoVazada(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        var match = SenhaNaConnectionString().Match(connectionString);

        if (!match.Success)
            return false;

        var senha = match.Groups[1].Value.Trim().Trim('\'', '"');
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(senha)));

        return hash.Equals(HashDaSenhaDeBancoVazada, StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"(?:^|;)\s*(?:Password|Pwd)\s*=\s*([^;]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SenhaNaConnectionString();
}
