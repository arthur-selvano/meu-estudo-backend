# Kanban em tempo real — design

Data: 2026-11-18
Status: aguardando aprovação

## Intenção

Hoje cada membro só vê mudanças no quadro depois de recarregar a página (F5).
Queremos que, quando alguém altera o responsável de um card, move, cria ou
conclui um card, todos os membros conectados àquele quadro vejam a mudança
imediatamente.

Junto com isso, a ação de alterar responsável precisa voltar a funcionar na
interface. Investigação: o endpoint `PATCH /api/topicos/{id}/responsavel`
está correto e testado ponta a ponta (200 ao atribuir/remover, 400 para quem
não participa). A API em execução na porta 5123, porém, roda um binário
anterior à criação da rota — por isso o frontend recebe 404. A correção é
recompilar/reiniciar a API; nenhuma mudança de código é necessária para isso.

## Objetivos

- Push instantâneo das mudanças de card para dono e membros aceitos do quadro.
- Cobertura de todas as mutações de card: criar, mover, alterar responsável e
  concluir.
- O cliente se reconecta sozinho e ressincroniza após queda de conexão.
- Nenhum membro de fora do quadro recebe eventos.

## Não-objetivos

- Notificar convites, criação/edição de quadro ou perfis.
- Indicador visual de "conectado"/presença de usuários.
- Fila offline ou garantia de entrega de eventos perdidos durante a desconexão
  (a ressincronização por refetch cobre esse caso).
- Alterar o modelo de domínio ou o banco.

## Abordagens consideradas

1. **SignalR (escolhida)** — push nativo do ASP.NET, entrega por usuário,
   reconexão automática. Custo: uma dependência nova no frontend
   (`@microsoft/signalr`). Nada de pacote novo no servidor: SignalR faz parte
   do framework compartilhado do ASP.NET.
2. **Polling de 3–5s** — sem dependências, mas com atraso visível, tráfego
   constante e risco de sobrescrever o estado otimista de um arraste em
   andamento.
3. **SSE** — push sem biblioteca no cliente, mas EventSource não envia header
   de autorização; exigiria token na query string e plumbing manual. SignalR
   resolve o mesmo problema com menos código.

## Como funciona (desenho)

### Backend

- `MeuEstudos.Api/Hubs/QuadroHub.cs`: hub `[Authorize]` vazio. A entrega é
  feita por `Clients.Users(...)`; não há grupos nem métodos de inscrição.
- `Program.cs`:
  - `AddSignalR()`; `MapHub<QuadroHub>("/hubs/quadro")`.
  - JWT via query string (`access_token`) apenas para `/hubs/quadro`, usando
    `JwtBearerEvents.OnMessageReceived` — WebSocket no browser não permite
    header `Authorization`.
  - CORS do frontend com `AllowCredentials()`.
- `MeuEstudos.Application/Interfaces/IQuadroNotificador.cs`: abstração para a
  camada de aplicação (que não pode referenciar a Api):
  `Task TopicoAtualizadoAsync(Guid grupoId, IReadOnlyCollection<Guid> destinatarios, TopicoEstudoResponse topico, CancellationToken ct)`.
- `MeuEstudos.Application/DTOs/TopicoAtualizadoEvent.cs`:
  `record TopicoAtualizadoEvent(Guid GrupoId, TopicoEstudoResponse Topico)`.
- `MeuEstudos.Api/Notifications/QuadroNotificador.cs`: implementa a abstração
  com `IHubContext<QuadroHub>`, enviando o evento `"TopicoAtualizado"` para
  `Clients.Users(destinatarios)`.
- `GrupoEstudoService`: após persistir, notifica nos quatro fluxos de card.
  Destinatários = `DonoId` + membros com status `Aceito` (helper privado).
  A notificação não deve derrubar a operação se falhar: erros de envio são
  registrados e engolidos.
- DI: registrar `IQuadroNotificador` na Api.

### Frontend

- `npm i @microsoft/signalr`.
- `src/services/realtimeService.js`: cria a conexão com
  `${API_ORIGIN}/hubs/quadro`, `accessTokenFactory` lendo o token do
  `localStorage`, `withAutomaticReconnect()`, e expõe
  `conectarTempoReal({ onTopicoAtualizado, onReconectado })` devolvendo uma
  função de encerramento.
- `Dashboard.jsx`: `useEffect` conecta após o login e desconecta no unmount.
  Ao receber `TopicoAtualizado`, faz upsert do card na lista plana
  `grupo.topicos` do grupo correspondente (substitui por id ou insere).
  Ao reconectar, chama `recarregarDados()` para cobrir eventos perdidos.
- Eventos do próprio usuário chegam também; o upsert é idempotente.

### Testes

- Backend: fake de `IQuadroNotificador` no `GrupoEstudoServiceTests`; asserts
  de que notificar acontece com grupo, destinatários (dono + aceitos) e card
  atualizado nos quatro fluxos, e que não notifica quem só tem convite
  pendente.
- Frontend: teste unitário da função de upsert do card (card existente é
  substituído; card novo é inserido; evento de outro grupo não altera nada).

## Decisões deixadas ao implementador

- Nome/assinatura exata do helper de destinatários e do evento no cliente.
- Se a reconexão usará `onreconnected` do SignalR ou o callback de
  `withAutomaticReconnect`.
- Tratamento de erro de conexão: silencioso (o quadro continua funcionando via
  ações normais) ou log no console.

## Operação

Após o merge, reiniciar a API. O binário atual em execução (porta 5123) não
contém a rota de responsável nem o hub.
