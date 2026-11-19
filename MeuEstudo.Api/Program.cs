using System.Globalization;
using System.Threading.RateLimiting;
using MeuEstudo.Api.Hubs;
using MeuEstudo.Api.Middlewares;
using MeuEstudo.Api.Notifications;
using MeuEstudo.Api.Security;
using MeuEstudo.Api.Services;
using MeuEstudo.Api.Settings;
using MeuEstudo.Application;
using MeuEstudo.Application.Interfaces;
using MeuEstudo.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    // Não revela a versão do servidor e limita o corpo total das requisições.
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 12 * 1024 * 1024;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Em produção a API fica atrás do proxy do Render. Sem isso, todos os
    // clientes compartilhariam o IP do proxy e o rate limit viraria um DoS global.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    options.ForwardLimit = 1;
});

builder.Services.AddControllers();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = false;
    options.MaximumReceiveMessageSize = 64 * 1024;
});
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IFotoPerfilService, FotoPerfilService>();
builder.Services.AddScoped<IArquivoPublicacaoService, ArquivoPublicacaoService>();
builder.Services.AddScoped<IQuadroNotificador, QuadroNotificador>();

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>()
    ?? throw new InvalidOperationException("A seção 'Jwt' não foi configurada.");

jwtSettings.Validar();

if (SegredosVazados.UsaSenhaDeBancoVazada(
        builder.Configuration.GetConnectionString("DefaultConnection")))
{
    throw new InvalidOperationException(
        "A senha do PostgreSQL configurada foi exposta publicamente no histórico do repositório. " +
        "Gere uma nova senha, rotacione o banco e configure-a apenas por variável de ambiente.");
}

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Mantém o mapeamento dos claims curtos do JWT para ClaimTypes.*.
        options.MapInboundClaims = true;
        options.TokenValidationParameters = JwtTokenValidation.CriarParametros(jwtSettings);
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // O browser não permite header Authorization no WebSocket do SignalR.
                // O token na query string é aceito SOMENTE no caminho do hub.
                var accessToken = context.Request.Query["access_token"];

                if (!string.IsNullOrEmpty(accessToken)
                    && context.HttpContext.Request.Path.StartsWithSegments("/hubs/quadro"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var origensPermitidas = (builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:5173", "https://meu-estudo-frontend.vercel.app"])
    .Select(origem => origem?.Trim().TrimEnd('/') ?? string.Empty)
    .Where(origem => origem.Length > 0)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (origensPermitidas.Length == 0)
    throw new InvalidOperationException("'Cors:AllowedOrigins' deve conter ao menos uma origem.");

foreach (var origem in origensPermitidas)
{
    if (origem == "*"
        || !Uri.TryCreate(origem, UriKind.Absolute, out var uri)
        || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
    {
        throw new InvalidOperationException(
            $"'Cors:AllowedOrigins' contém uma origem inválida: '{origem}'. " +
            "Use apenas origens absolutas http/https; '*' não é permitido com credenciais.");
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy
            .WithOrigins(origensPermitidas)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (contexto, cancellationToken) =>
    {
        if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            contexto.HttpContext.Response.Headers["Retry-After"] =
                Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        await contexto.HttpContext.Response.WriteAsJsonAsync(
            new { erro = "Muitas requisições. Aguarde um instante e tente novamente." },
            cancellationToken);
    };

    // Proteção geral contra abuso. É alta o bastante para não atrapalhar o uso
    // normal do Kanban em tempo real e baixa o suficiente para conter varreduras.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitions.PorUsuarioOuIp(contexto),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 600,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Login e cadastro: limita brute force e credential stuffing por IP.
    // Não bloqueia a conta, evitando que um atacante derrube um usuário legítimo.
    options.AddPolicy("auth", contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitions.PorIp(contexto),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Convites: evita spam de convites e enumeração de e-mails por um dono abusivo.
    options.AddPolicy("convite", contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitions.PorUsuarioOuIp(contexto),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Uploads de foto e anexos: limita consumo de disco e abuso de multipart.
    options.AddPolicy("upload", contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitions.PorUsuarioOuIp(contexto),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

// O diretório precisa existir no startup para o middleware de arquivos estáticos
// enxergar arquivos gravados depois.
var webRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
var uploadsPath = Path.Combine(webRootPath, "uploads");
Directory.CreateDirectory(Path.Combine(uploadsPath, "perfis"));
Directory.CreateDirectory(Path.Combine(uploadsPath, "publicacoes"));

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "MeuEstudo API v1"));
}

app.UseHttpsRedirection();

var extensoesDeImagem = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    ".png", ".jpg", ".jpeg", ".webp"
};

app.UseStaticFiles(new StaticFileOptions
{
    // Serve exclusivamente /uploads, nunca outros arquivos que venham a existir em wwwroot.
    RequestPath = "/uploads",
    FileProvider = new PhysicalFileProvider(uploadsPath),
    OnPrepareResponse = context =>
    {
        var headers = context.Context.Response.Headers;
        var caminho = context.Context.Request.Path;

        // Nomes são GUIDs gerados pelo servidor: o conteúdo nunca muda no mesmo caminho.
        headers["Cache-Control"] = "public, max-age=31536000, immutable";
        headers["X-Content-Type-Options"] = "nosniff";

        // Anexos de documentos são baixados, não renderizados inline pelo browser.
        // Imagens continuam inline para exibição normal no feed.
        if (caminho.StartsWithSegments("/uploads/publicacoes")
            && !extensoesDeImagem.Contains(Path.GetExtension(context.File.Name)))
        {
            headers["Content-Disposition"] = "attachment";
        }
    }
});

app.UseCors("Frontend");

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapHub<QuadroHub>("/hubs/quadro", options =>
{
    // Derruba a conexão quando o token expira, em vez de mantê-la indefinidamente.
    options.CloseOnAuthenticationExpiration = true;
});

app.Run();

/// <summary>Exposto para testes de integração (WebApplicationFactory).</summary>
public partial class Program { }
