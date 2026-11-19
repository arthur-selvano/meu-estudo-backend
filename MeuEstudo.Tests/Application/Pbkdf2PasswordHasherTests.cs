using System.Globalization;
using System.Security.Cryptography;
using MeuEstudo.Application.Services;

namespace MeuEstudo.Tests.Application;

public sealed class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_GeraSaltAleatorioEVerificaCorretamente()
    {
        var primeiro = _hasher.Hash("senha-super-secreta");
        var segundo = _hasher.Hash("senha-super-secreta");

        Assert.NotEqual(primeiro, segundo);
        Assert.True(_hasher.Verify("senha-super-secreta", primeiro));
        Assert.False(_hasher.Verify("senha-errada", primeiro));
        Assert.DoesNotContain("senha-super-secreta", primeiro);
    }

    [Fact]
    public void Hash_UsaOsParametrosAtuaisENaoPrecisaRehash()
    {
        var hash = _hasher.Hash("senha-super-secreta");
        var partes = hash.Split('$');

        Assert.Equal("pbkdf2-sha256", partes[0]);
        Assert.Equal("600000", partes[1]);
        Assert.False(_hasher.PrecisaRehash(hash));
    }

    [Fact]
    public void Hash_LegadoDe100k_ContinuaValidoEPrecisaRehash()
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var chave = Rfc2898DeriveBytes.Pbkdf2(
            "senha-antiga",
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        var legado = string.Join(
            '$',
            "pbkdf2-sha256",
            100_000.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(chave));

        Assert.True(_hasher.Verify("senha-antiga", legado));
        Assert.True(_hasher.PrecisaRehash(legado));
    }

    [Theory]
    [InlineData("")]
    [InlineData("senha-em-texto-puro")]
    [InlineData("pbkdf2-sha256$0$abc$def")]
    [InlineData("pbkdf2-sha256$999999999$abc$def")]
    [InlineData("outro-algoritmo$100000$abc$def")]
    public void Verify_RejeitaHashesMalformados(string hash)
    {
        Assert.False(_hasher.Verify("qualquer-senha", hash));
        Assert.True(_hasher.PrecisaRehash(hash));
    }
}
