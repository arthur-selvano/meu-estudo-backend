using MeuEstudo.Domain.Entities;
using MeuEstudo.Domain.Repositories;

namespace MeuEstudo.Tests.Fakes;

internal sealed class FakeGrupoEstudoRepository : IGrupoEstudoRepository
{
    private readonly Dictionary<Guid, GrupoEstudo> _grupos;

    public FakeGrupoEstudoRepository(params GrupoEstudo[] grupos)
    {
        _grupos = grupos.ToDictionary(grupo => grupo.Id);
    }

    public int Atualizacoes { get; private set; }

    public Task<GrupoEstudo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_grupos.GetValueOrDefault(id));

    public Task<GrupoEstudo?> ObterPorTopicoIdAsync(Guid topicoId, CancellationToken cancellationToken = default)
        => Task.FromResult(_grupos.Values.FirstOrDefault(grupo =>
            grupo.Topicos.Any(topico => topico.Id == topicoId)));

    public Task<IReadOnlyCollection<GrupoEstudo>> ObterPorDonoAsync(
        Guid donoId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<GrupoEstudo>>(
            _grupos.Values.Where(grupo => grupo.DonoId == donoId).ToList());

    public Task<IReadOnlyCollection<GrupoEstudo>> ObterDoUsuarioAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<GrupoEstudo>>(
            _grupos.Values.Where(grupo => grupo.DonoId == usuarioId).ToList());

    public Task<IReadOnlyCollection<GrupoEstudo>> ObterComConvitePendenteAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<GrupoEstudo>>(Array.Empty<GrupoEstudo>());

    public Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_grupos.ContainsKey(id));

    public Task AdicionarAsync(GrupoEstudo grupo, CancellationToken cancellationToken = default)
    {
        _grupos[grupo.Id] = grupo;
        return Task.CompletedTask;
    }

    public Task AtualizarAsync(GrupoEstudo grupo, CancellationToken cancellationToken = default)
    {
        Atualizacoes++;
        _grupos[grupo.Id] = grupo;
        return Task.CompletedTask;
    }

    public Task RemoverAsync(GrupoEstudo grupo, CancellationToken cancellationToken = default)
    {
        _grupos.Remove(grupo.Id);
        return Task.CompletedTask;
    }
}
