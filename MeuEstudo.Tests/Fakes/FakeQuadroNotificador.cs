using MeuEstudo.Application.DTOs;
using MeuEstudo.Application.Interfaces;

namespace MeuEstudo.Tests.Fakes;

internal sealed class FakeQuadroNotificador : IQuadroNotificador
{
    public List<NotificacaoTopico> Notificacoes { get; } = new();

    public Task TopicoAtualizadoAsync(
        Guid grupoId,
        IReadOnlyCollection<Guid> destinatarios,
        TopicoEstudoResponse topico,
        CancellationToken cancellationToken = default)
    {
        Notificacoes.Add(new NotificacaoTopico(grupoId, destinatarios, topico));
        return Task.CompletedTask;
    }
}

internal sealed record NotificacaoTopico(
    Guid GrupoId,
    IReadOnlyCollection<Guid> Destinatarios,
    TopicoEstudoResponse Topico);
