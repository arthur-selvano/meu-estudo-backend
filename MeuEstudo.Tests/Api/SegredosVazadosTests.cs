using MeuEstudo.Api.Security;

namespace MeuEstudo.Tests.Api;

public sealed class SegredosVazadosTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Host=localhost;Port=5432;Database=db;Username=user")]
    [InlineData("Host=localhost;Port=5432;Database=db;Username=user;Password=uma-senha-nova")]
    public void UsaSenhaDeBancoVazada_NaoBloqueiaCredenciaisNovas(string? connectionString)
    {
        Assert.False(SegredosVazados.UsaSenhaDeBancoVazada(connectionString));
    }
}
