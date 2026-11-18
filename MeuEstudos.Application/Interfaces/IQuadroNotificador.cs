using MeuEstudos.Application.DTOs;

namespace MeuEstudos.Application.Interfaces;

/// <summary>
/// Publica mudanças de cards para os membros conectados ao quadro.
/// A implementação fica na API (SignalR); falhas de envio não devem derrubar a operação.
/// </summary>
public interface IQuadroNotificador
{
    Task TopicoAtualizadoAsync(
        Guid grupoId,
        IReadOnlyCollection<Guid> destinatarios,
        TopicoEstudoResponse topico,
        CancellationToken cancellationToken = default);
}
