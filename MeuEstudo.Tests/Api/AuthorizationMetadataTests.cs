using MeuEstudo.Api.Controllers;
using MeuEstudo.Api.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;

namespace MeuEstudo.Tests.Api;

/// <summary>
/// Verifica no assembly da API que endpoints e hubs continuam protegidos e que
/// as políticas de rate limit estão aplicadas onde foram definidas.
/// </summary>
public sealed class AuthorizationMetadataTests
{
    [Theory]
    [InlineData(typeof(GruposController))]
    [InlineData(typeof(PerfisController))]
    [InlineData(typeof(PublicacoesController))]
    [InlineData(typeof(TopicosController))]
    public void ControllersDeDados_ExigemAutenticacao(Type controller)
    {
        Assert.NotNull(controller.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
    }

    [Fact]
    public void AuthController_NaoExigeAutenticacaoNoNivelDaClasse()
    {
        Assert.Null(typeof(AuthController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
    }

    [Fact]
    public void HubDoQuadro_ExigeAutenticacao()
    {
        Assert.NotNull(typeof(QuadroHub).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
        Assert.True(typeof(Hub).IsAssignableFrom(typeof(QuadroHub)));
    }

    [Theory]
    [InlineData(typeof(AuthController), "Login", "auth")]
    [InlineData(typeof(AuthController), "Registrar", "auth")]
    [InlineData(typeof(GruposController), "ConvidarMembro", "convite")]
    [InlineData(typeof(PerfisController), "EnviarFoto", "upload")]
    [InlineData(typeof(PublicacoesController), "Criar", "upload")]
    public void AcoesSensiveis_UsamAPoliticaDeRateLimitEsperada(
        Type controller,
        string acao,
        string politica)
    {
        var metodo = controller.GetMethod(acao);

        Assert.NotNull(metodo);

        // A política pode estar no método ou herdada do controller.
        var atributo = metodo!
            .GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: true)
            .Cast<EnableRateLimitingAttribute>()
            .SingleOrDefault()
            ?? controller
                .GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: true)
                .Cast<EnableRateLimitingAttribute>()
                .SingleOrDefault();

        Assert.NotNull(atributo);
        Assert.Equal(politica, atributo!.PolicyName);
    }
}
