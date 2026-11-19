namespace MeuEstudo.Api.Settings;

/// <summary>Configuração da emissão e validação dos tokens JWT.</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
}
