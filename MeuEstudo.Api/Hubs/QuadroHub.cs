using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MeuEstudo.Api.Hubs;

/// <summary>
/// Conexão autenticada usada apenas para receber atualizações do quadro.
/// A entrega é feita por usuário (<c>Clients.Users</c>); não há métodos de inscrição
/// nem grupos controlados pelo cliente, então o cliente não escolhe o que recebe.
/// </summary>
[Authorize]
public sealed class QuadroHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Defesa adicional: o token válido precisa identificar um usuário real.
        var usuarioId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(usuarioId, out _))
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }
}
