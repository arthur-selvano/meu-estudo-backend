using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;

namespace MeuEstudo.Tests.Api;

/// <summary>
/// Testes de integração da pipeline HTTP real: autenticação, 401, rate limit,
/// CORS e cabeçalhos de segurança. Não dependem de um PostgreSQL em execução
/// porque as requisições são barradas antes de qualquer acesso ao banco.
/// </summary>
public sealed class ApiSecurityIntegrationTests
{
    private const string ChaveValida = "chave-de-teste-com-64-caracteres-para-hs256-aaaaaaaaaaaaaaaaaaaa";
    private const string OutraChave = "outra-chave-de-teste-64-caracteres-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    private static WebApplicationFactory<Program> CriarFactory()
        => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseContentRoot(AppContext.BaseDirectory);
            builder.UseSetting("Jwt:Key", ChaveValida);
            builder.UseSetting("Jwt:Issuer", "MeuEstudo.Api");
            builder.UseSetting("Jwt:Audience", "MeuEstudo.Frontend");
            builder.UseSetting("Jwt:ExpiresMinutes", "120");
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Host=127.0.0.1;Port=5432;Database=teste;Username=teste;Password=teste");
            builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:5173");
            builder.UseSetting("Cors:AllowedOrigins:1", "https://meu-estudo-frontend.vercel.app");
        });

    private static string GerarToken(DateTime expiraEm, string chave = ChaveValida)
    {
        var credenciais = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "MeuEstudo.Api",
            audience: "MeuEstudo.Frontend",
            claims: [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            notBefore: DateTime.UtcNow.AddMinutes(-20),
            expires: expiraEm,
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task EndpointProtegido_SemToken_Retorna401()
    {
        using var factory = CriarFactory();
        using var client = factory.CreateClient();

        var resposta = await client.GetAsync("/api/grupos");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task EndpointProtegido_ComTokenExpirado_Retorna401()
    {
        using var factory = CriarFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GerarToken(DateTime.UtcNow.AddMinutes(-10)));

        var resposta = await client.GetAsync("/api/grupos");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task EndpointProtegido_ComAssinaturaIncorreta_Retorna401()
    {
        using var factory = CriarFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                GerarToken(DateTime.UtcNow.AddMinutes(10), OutraChave));

        var resposta = await client.GetAsync("/api/grupos");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task HubSignalR_SemToken_NaoNegociaConexao()
    {
        using var factory = CriarFactory();
        using var client = factory.CreateClient();

        var resposta = await client.PostAsync("/hubs/quadro/negotiate?negotiateVersion=1", null);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task RespostasDaApi_IncluemCabecalhosDeSeguranca()
    {
        using var factory = CriarFactory();
        using var client = factory.CreateClient();

        var resposta = await client.GetAsync("/api/grupos");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal("nosniff", Assert.Single(resposta.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(resposta.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("no-referrer", Assert.Single(resposta.Headers.GetValues("Referrer-Policy")));
        Assert.Contains("default-src 'none'", Assert.Single(resposta.Headers.GetValues("Content-Security-Policy")));
        Assert.Equal("no-store", Assert.Single(resposta.Headers.GetValues("Cache-Control")));
    }

    [Fact]
    public async Task Cors_NaoLiberaOrigemDesconhecida()
    {
        using var factory = CriarFactory();
        using var client = factory.CreateClient();

        using var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/grupos");
        requisicao.Headers.Add("Origin", "https://invasor.example");

        var resposta = await client.SendAsync(requisicao);

        Assert.False(resposta.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Cors_LiberaSomenteAOrigemConfigurada()
    {
        using var factory = CriarFactory();
        using var client = factory.CreateClient();

        using var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/grupos");
        requisicao.Headers.Add("Origin", "https://meu-estudo-frontend.vercel.app");

        var resposta = await client.SendAsync(requisicao);

        Assert.Equal(
            "https://meu-estudo-frontend.vercel.app",
            Assert.Single(resposta.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Login_AcimaDoLimiteDeTentativas_Retorna429()
    {
        using var factory = CriarFactory();
        using var client = factory.CreateClient();

        var respostas = new List<HttpStatusCode>();

        for (var tentativa = 0; tentativa < 11; tentativa++)
        {
            using var conteudo = new StringContent("{", Encoding.UTF8, "application/json");
            using var resposta = await client.PostAsync("/api/auth/login", conteudo);
            respostas.Add(resposta.StatusCode);
        }

        Assert.All(respostas.Take(10), status => Assert.Equal(HttpStatusCode.BadRequest, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, respostas[^1]);
    }
}
