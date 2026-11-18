using MeuEstudos.Api.Hubs;
using MeuEstudos.Application.DTOs;
using MeuEstudos.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace MeuEstudos.Api.Notifications;

public sealed class QuadroNotificador : IQuadroNotificador
{
    private const string Evento = "TopicoAtualizado";

    private readonly IHubContext<QuadroHub> _hubContext;
    private readonly ILogger<QuadroNotificador> _logger;

    public QuadroNotificador(
        IHubContext<QuadroHub> hubContext,
        ILogger<QuadroNotificador> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task TopicoAtualizadoAsync(
        Guid grupoId,
        IReadOnlyCollection<Guid> destinatarios,
        TopicoEstudoResponse topico,
        CancellationToken cancellationToken = default)
    {
        if (destinatarios.Count == 0)
            return;

        try
        {
            await _hubContext.Clients
                .Users(destinatarios.Select(id => id.ToString()).ToArray())
                .SendAsync(Evento, new TopicoAtualizadoEvent(grupoId, topico), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao notificar o grupo {GrupoId} sobre o tópico {TopicoId}.",
                grupoId,
                topico.Id);
        }
    }
}
