using System.Security.Cryptography;
using System.Text;

namespace MeuEstudo.Api.Settings;

/// <summary>Configuração da emissão e validação dos tokens JWT.</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    private const int KeyMinBytes = 32;
    private const int ValidAtéMinutos = 1440;

    /// <summary>
    /// Hash SHA-256 da antiga chave de desenvolvimento que ficou exposta no
    /// repositório público. A chave em texto puro não é repetida aqui.
    /// </summary>
    private const string HashDaChaveVazada =
        "f78b69ff16c8e9cbbf0b3d6acd4c4f5a199216f2ad5735a2fce29405b0185093";

    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;

    /// <summary>Validade do token em minutos. Padrão: 120 (2 horas).</summary>
    public int ExpiresMinutes { get; init; } = 120;

    /// <summary>
    /// Valida a configuração no startup. Falhar cedo evita subir a API com
    /// autenticação fraca ou sem emissor/audiência.
    /// </summary>
    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(Key) || Encoding.UTF8.GetByteCount(Key) < KeyMinBytes)
        {
            throw new InvalidOperationException(
                "'Jwt:Key' deve ser configurada com pelo menos 32 bytes. " +
                "Defina a variável de ambiente Jwt__Key.");
        }

        var hashDaChave = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Key)));

        if (hashDaChave.Equals(HashDaChaveVazada, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "A chave JWT configurada foi exposta publicamente no histórico do repositório " +
                "e não pode mais ser usada. Gere uma nova chave forte e rotacione os tokens.");
        }

        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("'Jwt:Issuer' e 'Jwt:Audience' devem ser configurados.");

        if (ExpiresMinutes is < 5 or > ValidAtéMinutos)
        {
            throw new InvalidOperationException(
                $"'Jwt:ExpiresMinutes' deve estar entre 5 e {ValidAtéMinutos} minutos.");
        }
    }
}
