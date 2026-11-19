using MeuEstudo.Application.DTOs;
using MeuEstudo.Application.Interfaces;
using MeuEstudo.Application.Services;
using MeuEstudo.Domain.Entities;
using MeuEstudo.Domain.Exceptions;
using MeuEstudo.Domain.Repositories;

namespace MeuEstudo.Tests.Application;

public sealed class AuthServiceTests
{
    private const string CredenciaisInvalidas = "E-mail ou senha inválidos.";

    [Fact]
    public async Task LoginAsync_EmailInexistente_UsaMesmaMensagemEExecutaHashDescartavel()
    {
        var repositorio = new RepositorioFake { UsuarioPorEmail = null };
        var hasher = new HasherFake();
        var servico = new AuthService(repositorio, hasher);

        var excecao = await Assert.ThrowsAsync<DomainException>(
            () => servico.LoginAsync(new LoginRequest("naoexiste@exemplo.com", "senha-valida")));

        Assert.Equal(CredenciaisInvalidas, excecao.Message);
        Assert.Equal(1, hasher.Hashes);
        Assert.Equal(0, hasher.Verificacoes);
    }

    [Fact]
    public async Task LoginAsync_SenhaIncorreta_RetornaMensagemGenerica()
    {
        var usuario = new Usuario("Fulano", "fulano@exemplo.com", "hash-antigo");
        var repositorio = new RepositorioFake { UsuarioPorEmail = usuario };
        var hasher = new HasherFake { Verificar = false };
        var servico = new AuthService(repositorio, hasher);

        var excecao = await Assert.ThrowsAsync<DomainException>(
            () => servico.LoginAsync(new LoginRequest("fulano@exemplo.com", "senha-errada")));

        Assert.Equal(CredenciaisInvalidas, excecao.Message);
        Assert.Equal(1, hasher.Verificacoes);
        Assert.Equal(0, repositorio.Atualizacoes);
    }

    [Fact]
    public async Task LoginAsync_SenhaAcimaDoLimite_NaoConsultaRepositorio()
    {
        var repositorio = new RepositorioFake();
        var hasher = new HasherFake();
        var servico = new AuthService(repositorio, hasher);

        var excecao = await Assert.ThrowsAsync<DomainException>(
            () => servico.LoginAsync(new LoginRequest("fulano@exemplo.com", new string('a', 129))));

        Assert.Equal(CredenciaisInvalidas, excecao.Message);
        Assert.Equal(0, repositorio.Consultas);
        Assert.Equal(0, hasher.Verificacoes);
    }

    [Fact]
    public async Task LoginAsync_HashAntigo_AtualizaParaOsParametrosAtuais()
    {
        var usuario = new Usuario("Fulano", "fulano@exemplo.com", "pbkdf2-sha256$100000$salt$chave");
        var repositorio = new RepositorioFake { UsuarioPorEmail = usuario };
        var hasher = new HasherFake { Verificar = true, Rehash = true };
        var servico = new AuthService(repositorio, hasher);

        await servico.LoginAsync(new LoginRequest("fulano@exemplo.com", "senha-valida"));

        Assert.Equal("hash-1", usuario.SenhaHash);
        Assert.Equal(1, repositorio.Atualizacoes);
    }

    [Fact]
    public async Task RegistrarAsync_SenhaCurta_RejeitaSemPersistir()
    {
        var repositorio = new RepositorioFake();
        var servico = new AuthService(repositorio, new HasherFake());

        await Assert.ThrowsAsync<DomainException>(
            () => servico.RegistrarAsync(new RegistrarUsuarioRequest("Fulano", "fulano@exemplo.com", "1234567")));

        Assert.False(repositorio.Persistido);
    }

    [Fact]
    public async Task RegistrarAsync_EmailDuplicado_RejeitaSemPersistir()
    {
        var repositorio = new RepositorioFake { EmailExiste = true };
        var servico = new AuthService(repositorio, new HasherFake());

        await Assert.ThrowsAsync<DomainException>(
            () => servico.RegistrarAsync(new RegistrarUsuarioRequest("Fulano", "fulano@exemplo.com", "senha-valida")));

        Assert.False(repositorio.Persistido);
    }

    [Fact]
    public async Task RegistrarAsync_DadosValidos_PersisteSomenteOHash()
    {
        var repositorio = new RepositorioFake();
        var servico = new AuthService(repositorio, new HasherFake());

        var resposta = await servico.RegistrarAsync(
            new RegistrarUsuarioRequest("Fulano", "fulano@exemplo.com", "senha-valida"));

        Assert.True(repositorio.Persistido);
        Assert.Equal("hash-1", repositorio.UsuarioPorEmail!.SenhaHash);
        Assert.NotEqual("senha-valida", repositorio.UsuarioPorEmail.SenhaHash);
        Assert.Equal("fulano@exemplo.com", resposta.Email);
    }

    private sealed class HasherFake : IPasswordHasher
    {
        public bool Verificar { get; init; } = true;
        public bool Rehash { get; init; }
        public int Hashes { get; private set; }
        public int Verificacoes { get; private set; }

        public string Hash(string senha)
        {
            Hashes++;
            return $"hash-{Hashes}";
        }

        public bool Verify(string senha, string hash)
        {
            Verificacoes++;
            return Verificar;
        }

        public bool PrecisaRehash(string hash) => Rehash;
    }

    private sealed class RepositorioFake : IUsuarioRepository
    {
        public Usuario? UsuarioPorEmail { get; set; }
        public bool EmailExiste { get; init; }
        public bool Persistido { get; private set; }
        public int Atualizacoes { get; private set; }
        public int Consultas { get; private set; }

        public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Usuario?>(null);

        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            Consultas++;
            return Task.FromResult(UsuarioPorEmail);
        }

        public Task<IReadOnlyCollection<Usuario>> ObterTodosAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<Usuario>>(Array.Empty<Usuario>());

        public Task<IReadOnlyDictionary<Guid, string>> ObterNomesPorIdsAsync(
            IEnumerable<Guid> ids,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<IReadOnlyDictionary<Guid, UsuarioResumo>> ObterResumosPorIdsAsync(
            IEnumerable<Guid> ids,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<Guid, UsuarioResumo>>(
                new Dictionary<Guid, UsuarioResumo>());

        public Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(EmailExiste);

        public Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            Persistido = true;
            UsuarioPorEmail = usuario;
            return Task.CompletedTask;
        }

        public Task AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            Atualizacoes++;
            return Task.CompletedTask;
        }
    }
}
