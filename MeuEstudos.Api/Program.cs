using System.Text;
using MeuEstudos.Api.Hubs;
using MeuEstudos.Api.Middlewares;
using MeuEstudos.Api.Notifications;
using MeuEstudos.Api.Services;
using MeuEstudos.Api.Settings;
using MeuEstudos.Application;
using MeuEstudos.Application.Interfaces;
using MeuEstudos.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
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

if (string.IsNullOrWhiteSpace(jwtSettings.Key) || Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
    throw new InvalidOperationException("'Jwt:Key' deve ter pelo menos 32 caracteres.");

if (string.IsNullOrWhiteSpace(jwtSettings.Issuer) || string.IsNullOrWhiteSpace(jwtSettings.Audience))
    throw new InvalidOperationException("'Jwt:Issuer' e 'Jwt:Audience' devem ser configurados.");

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Mantém o mapeamento dos claims curtos do JWT para ClaimTypes.*.
        options.MapInboundClaims = true;
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // O browser não permite header Authorization no WebSocket do SignalR.
                var accessToken = context.Request.Query["access_token"];

                if (!string.IsNullOrEmpty(accessToken)
                    && context.HttpContext.Request.Path.StartsWithSegments("/hubs/quadro"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// O diretório precisa existir no startup para o middleware de arquivos estáticos
// enxergar arquivos gravados depois.
var webRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(webRootPath, "uploads", "perfis"));
Directory.CreateDirectory(Path.Combine(webRootPath, "uploads", "publicacoes"));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "MeuEstudos API v1"));
}

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(webRootPath)
});

app.UseCors("FrontendDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<QuadroHub>("/hubs/quadro");

app.Run();
