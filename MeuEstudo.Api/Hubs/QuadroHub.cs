using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MeuEstudo.Api.Hubs;

/// <summary>
/// Conexão autenticada usada apenas para receber atualizações do quadro.
/// A entrega é feita por usuário (<c>Clients.Users</c>); não há métodos de inscrição.
/// </summary>
[Authorize]
public sealed class QuadroHub : Hub
{
}
