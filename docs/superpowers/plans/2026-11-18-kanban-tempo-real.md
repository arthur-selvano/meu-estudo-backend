# Kanban em tempo real — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Notificar em tempo real (SignalR) dono e membros aceitos quando um card do quadro é criado, movido, tem o responsável alterado ou é concluído, sem o cliente precisar recarregar a página.

**Architecture:** A camada de aplicação publica o evento por uma abstração (`IQuadroNotificador`); a API implementa essa abstração com `IHubContext<QuadroHub>` e entrega por `Clients.Users`, ou seja, sem grupos nem inscrição no cliente. O frontend abre uma conexão autenticada em `/hubs/quadro`, recebe `TopicoAtualizado` e faz upsert do card no estado do quadro.

**Tech Stack:** .NET 10 (ASP.NET Core SignalR, xUnit), React 19 + Vite, `@microsoft/signalr`.

**Spec:** `docs/superpowers/specs/2026-11-18-kanban-tempo-real-design.md`

## Global Constraints

- Servidor: nenhum pacote NuGet novo; SignalR já faz parte do framework compartilhado do ASP.NET Core.
- Cliente: única dependência nova é `@microsoft/signalr`.
- Nome do evento no hub: exatamente `TopicoAtualizado`.
- Destinatários: `DonoId` + membros com status `Aceito`, sem duplicatas. Convites `Pendente`/`Recusado` nunca recebem.
- Código e comentários em português, seguindo o estilo existente.
- **Não criar commits**: o usuário não autorizou. Deixar as alterações no working tree.
- Após mudanças no backend, a API em execução precisa ser recompilada e reiniciada.

## Review Focus

- Convite ainda pendente recebendo eventos do quadro → Task 1 testa que só dono + aceitos estão em `Destinatarios`.
- Evento de um quadro alterando outro quadro no estado do React → Task 3 testa que grupos com id diferente ficam intocados.
- Eco do próprio evento ou evento duplicado duplicando o card → Task 3 testa que aplicar o mesmo evento duas vezes mantém um único card.
- Card movido continuar aparecendo na coluna antiga para os outros → Task 3 testa que o upsert substitui `colunaId`; Task 4 valida no app.
- API/hub indisponível impedindo criar/mover cards → Task 3 faz a conexão falhar em silêncio; Task 4 valida que as ações normais continuam funcionando.

---

### Task 1: Aplicação publica mudanças de card

**Files:**
- Create: `MeuEstudos.Application/DTOs/TopicoAtualizadoEvent.cs`
- Create: `MeuEstudos.Application/Interfaces/IQuadroNotificador.cs`
- Modify: `MeuEstudos.Application/Services/GrupoEstudoService.cs` (construtor + os quatro métodos de card + helper de destinatários)
- Create: `MeuEstudos.Tests/Fakes/FakeQuadroNotificador.cs`
- Test: `MeuEstudos.Tests/Application/GrupoEstudoServiceTests.cs`

**Interfaces:**
- Consumes: `TopicoEstudoResponse`, `GrupoEstudo`, `MembroGrupo.EhMembroAceito()`.
- Produces:
  ```csharp
  public sealed record TopicoAtualizadoEvent(Guid GrupoId, TopicoEstudoResponse Topico);

  public interface IQuadroNotificador
  {
      Task TopicoAtualizadoAsync(
          Guid grupoId,
          IReadOnlyCollection<Guid> destinatarios,
          TopicoEstudoResponse topico,
          CancellationToken cancellationToken = default);
  }
  ```

- [ ] **Step 1: Escrever o fake e os testes que falham**

Criar `MeuEstudos.Tests/Fakes/FakeQuadroNotificador.cs`:

```csharp
using MeuEstudos.Application.DTOs;
using MeuEstudos.Application.Interfaces;

namespace MeuEstudos.Tests.Fakes;

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
```

Em `GrupoEstudoServiceTests.cs`, acrescentar `new FakeQuadroNotificador()` como terceiro argumento nas quatro construções existentes (`linhas 19, 51, 76, 95`) e adicionar os quatro testes abaixo:

```csharp
[Fact]
public async Task AlterarResponsavelTopicoAsync_NotificaSomenteDonoEMembrosAceitos()
{
    var donoId = Guid.Parse("d0642713-a0e3-4fbf-a486-ded7452fa975");
    var membroAceitoId = Guid.Parse("0b5f8f03-2a84-4c8d-9f5f-5c3e1f2a7b11");
    var membroPendenteId = Guid.Parse("4f0f0d6e-6a78-4a52-9f6e-9a4d1f3c2b22");
    var grupo = new GrupoEstudo("Grupo", null, TipoGrupo.TrabalhoAcademico, donoId);
    grupo.ConvidarMembro(donoId, membroAceitoId);
    grupo.ResponderConvite(membroAceitoId, true);
    grupo.ConvidarMembro(donoId, membroPendenteId);
    var topico = grupo.AdicionarTopico(
        donoId,
        grupo.Colunas.OrderBy(coluna => coluna.Ordem).First().Id,
        "Card");

    var notificador = new FakeQuadroNotificador();
    var servico = new GrupoEstudoService(
        new FakeGrupoEstudoRepository(grupo),
        new FakeUsuarioRepository(),
        notificador);

    await servico.AlterarResponsavelTopicoAsync(
        donoId,
        topico.Id,
        new AlterarResponsavelTopicoRequest(membroAceitoId));

    var notificacao = Assert.Single(notificador.Notificacoes);
    Assert.Equal(grupo.Id, notificacao.GrupoId);
    Assert.Contains(donoId, notificacao.Destinatarios);
    Assert.Contains(membroAceitoId, notificacao.Destinatarios);
    Assert.DoesNotContain(membroPendenteId, notificacao.Destinatarios);
    Assert.Equal(membroAceitoId, notificacao.Topico.ResponsavelId);
}

[Fact]
public async Task MoverTopicoAsync_NotificaCardNaColunaDeDestino()
{
    var donoId = Guid.Parse("d0642713-a0e3-4fbf-a486-ded7452fa975");
    var grupo = new GrupoEstudo("Grupo", null, TipoGrupo.TrabalhoAcademico, donoId);
    var colunas = grupo.Colunas.OrderBy(coluna => coluna.Ordem).ToList();
    var topico = grupo.AdicionarTopico(donoId, colunas[0].Id, "Card");

    var notificador = new FakeQuadroNotificador();
    var servico = new GrupoEstudoService(
        new FakeGrupoEstudoRepository(grupo),
        new FakeUsuarioRepository(),
        notificador);

    await servico.MoverTopicoAsync(
        donoId,
        topico.Id,
        new MoverTopicoRequest(colunas[1].Id, 0));

    var notificacao = Assert.Single(notificador.Notificacoes);
    Assert.Equal(colunas[1].Id, notificacao.Topico.ColunaId);
    Assert.Contains(donoId, notificacao.Destinatarios);
}

[Fact]
public async Task AdicionarTopicoAsync_NotificaCardCriado()
{
    var donoId = Guid.Parse("d0642713-a0e3-4fbf-a486-ded7452fa975");
    var grupo = new GrupoEstudo("Grupo", null, TipoGrupo.TrabalhoAcademico, donoId);
    var coluna = grupo.Colunas.OrderBy(coluna => coluna.Ordem).First();

    var notificador = new FakeQuadroNotificador();
    var servico = new GrupoEstudoService(
        new FakeGrupoEstudoRepository(grupo),
        new FakeUsuarioRepository(),
        notificador);

    var resposta = await servico.AdicionarTopicoAsync(
        donoId,
        grupo.Id,
        new CriarTopicoRequest("Card novo", coluna.Id));

    var notificacao = Assert.Single(notificador.Notificacoes);
    Assert.Equal(resposta.Id, notificacao.Topico.Id);
    Assert.Equal(grupo.Id, notificacao.GrupoId);
}

[Fact]
public async Task AlternarConclusaoTopicoAsync_NotificaConclusao()
{
    var donoId = Guid.Parse("d0642713-a0e3-4fbf-a486-ded7452fa975");
    var grupo = new GrupoEstudo("Grupo", null, TipoGrupo.TrabalhoAcademico, donoId);
    var topico = grupo.AdicionarTopico(
        donoId,
        grupo.Colunas.OrderBy(coluna => coluna.Ordem).First().Id,
        "Card");

    var notificador = new FakeQuadroNotificador();
    var servico = new GrupoEstudoService(
        new FakeGrupoEstudoRepository(grupo),
        new FakeUsuarioRepository(),
        notificador);

    await servico.AlternarConclusaoTopicoAsync(donoId, grupo.Id, topico.Id);

    var notificacao = Assert.Single(notificador.Notificacoes);
    Assert.True(notificacao.Topico.Concluido);
}
```

- [ ] **Step 2: Rodar os testes e confirmar que falham**

Run: `dotnet test --nologo`
Expected: FAIL — `IQuadroNotificador`/`FakeQuadroNotificador` não compilam e `GrupoEstudoService` não aceita três argumentos.

- [ ] **Step 3: Criar o DTO e a abstração**

`MeuEstudos.Application/DTOs/TopicoAtualizadoEvent.cs`:

```csharp
namespace MeuEstudos.Application.DTOs;

/// <summary>Evento enviado em tempo real quando um card do quadro é criado ou alterado.</summary>
public sealed record TopicoAtualizadoEvent(Guid GrupoId, TopicoEstudoResponse Topico);
```

`MeuEstudos.Application/Interfaces/IQuadroNotificador.cs`:

```csharp
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
```

- [ ] **Step 4: Injetar e chamar o notificador em `GrupoEstudoService`**

No construtor, adicionar o campo `private readonly IQuadroNotificador _quadroNotificador;` e o parâmetro `IQuadroNotificador quadroNotificador` (com `?? throw new ArgumentNullException(...)`), mantendo os dois parâmetros atuais.

Adicionar o helper:

```csharp
private static IReadOnlyCollection<Guid> DestinatariosDoQuadro(GrupoEstudo grupo)
    => grupo.Membros
        .Where(membro => membro.EhMembroAceito())
        .Select(membro => membro.UsuarioId)
        .Append(grupo.DonoId)
        .Distinct()
        .ToList();
```

Nos métodos `AdicionarTopicoAsync`, `AlternarConclusaoTopicoAsync`, `MoverTopicoAsync` e `AlterarResponsavelTopicoAsync`, trocar o final:

```csharp
var usuarios = await ObterResumosDoResponsavelAsync(topico, cancellationToken);
return MapearTopico(topico, usuarios);
```

por:

```csharp
var usuarios = await ObterResumosDoResponsavelAsync(topico, cancellationToken);
var resposta = MapearTopico(topico, usuarios);

await _quadroNotificador.TopicoAtualizadoAsync(
    grupo.Id,
    DestinatariosDoQuadro(grupo),
    resposta,
    cancellationToken);

return resposta;
```

- [ ] **Step 5: Rodar os testes e confirmar que passam**

Run: `dotnet test --nologo`
Expected: PASS — 24 testes aprovados.

---

### Task 2: API expõe o hub SignalR autenticado

**Files:**
- Create: `MeuEstudos.Api/Hubs/QuadroHub.cs`
- Create: `MeuEstudos.Api/Notifications/QuadroNotificador.cs`
- Modify: `MeuEstudos.Api/Program.cs`

**Interfaces:**
- Consumes: `IQuadroNotificador` e `TopicoAtualizadoEvent` da Task 1.
- Produces: hub em `/hubs/quadro`; evento `TopicoAtualizado` com payload camelCase `{ grupoId, topico }`.

- [ ] **Step 1: Criar o hub e o notificador**

`MeuEstudos.Api/Hubs/QuadroHub.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MeuEstudos.Api.Hubs;

/// <summary>
/// Conexão autenticada usada apenas para receber atualizações do quadro.
/// A entrega é feita por usuário (<c>Clients.Users</c>); não há métodos de inscrição.
/// </summary>
[Authorize]
public sealed class QuadroHub : Hub
{
}
```

`MeuEstudos.Api/Notifications/QuadroNotificador.cs`:

```csharp
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
```

- [ ] **Step 2: Configurar SignalR, JWT na query string e DI em `Program.cs`**

Adicionar `using MeuEstudos.Api.Hubs;` e `using MeuEstudos.Api.Notifications;` no topo.

Após `builder.Services.AddControllers();`, adicionar:

```csharp
builder.Services.AddSignalR();
```

Na policy `FrontendDev`, adicionar `AllowCredentials()` ao final da cadeia.

No `AddJwtBearer`, depois de `MapInboundClaims`, registrar o evento:

```csharp
options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        var accessToken = context.Request.Query["access_token"];

        if (!string.IsNullOrEmpty(accessToken)
            && context.HttpContext.Request.Path.StartsWithSegments("/hubs/quadro"))
        {
            context.Token = accessToken;
        }

        return Task.CompletedTask;
    }
};
```

Registrar o notificador junto aos outros serviços de escopo:

```csharp
builder.Services.AddScoped<IQuadroNotificador, QuadroNotificador>();
```

Depois de `app.MapControllers();`, adicionar:

```csharp
app.MapHub<QuadroHub>("/hubs/quadro");
```

- [ ] **Step 3: Compilar**

Run: `dotnet build MeuEstudos.Api/MeuEstudos.Api.csproj --nologo`
Expected: 0 erros, 0 avisos.

- [ ] **Step 4: Verificar o handshake autenticado**

Subir a API (ex.: porta 5124), registrar um usuário e executar:

```bash
curl -i -X POST 'http://localhost:5124/hubs/quadro/negotiate?negotiateVersion=1' \
  -H "Authorization: Bearer <token>"
```

Expected: `HTTP/1.1 200` com `connectionToken`; sem o header, `401`.

---

### Task 3: Cliente SignalR e upsert do card

**Files:**
- Modify: `meu-estudo-frontend/package.json` (via `npm install @microsoft/signalr`)
- Create: `meu-estudo-frontend/src/services/realtimeService.js`
- Modify: `meu-estudo-frontend/src/utils/quadro.js`
- Test: `meu-estudo-frontend/src/utils/quadro.test.js`

**Interfaces:**
- Produces:
  ```js
  conectarTempoReal({ onTopicoAtualizado, onReconectado }) // retorna () => void
  atualizarTopicoNoGrupo(grupos, grupoId, topico) // retorna novo array de grupos
  ```
- Consumes: `API_ORIGIN` e `TOKEN_KEY` de `src/services/api.js`; evento `{ grupoId, topico }`.

- [ ] **Step 1: Instalar a dependência**

Run: `cd meu-estudo-frontend && npm install @microsoft/signalr`
Expected: `package.json` e `package-lock.json` atualizados.

- [ ] **Step 2: Escrever os testes que falham em `src/utils/quadro.test.js`**

Adicionar `atualizarTopicoNoGrupo` ao import de `./quadro.js` e os testes:

```js
const GRUPO_A = {
  id: 'grupo-a',
  topicos: [
    { id: 'card-1', titulo: 'Card 1', colunaId: 'coluna-1' },
    { id: 'card-2', titulo: 'Card 2', colunaId: 'coluna-1' },
  ],
}

const GRUPO_B = {
  id: 'grupo-b',
  topicos: [{ id: 'card-9', titulo: 'Card 9', colunaId: 'coluna-x' }],
}

test('atualizarTopicoNoGrupo substitui o card existente sem duplicar', () => {
  const grupos = atualizarTopicoNoGrupo([GRUPO_A], 'grupo-a', {
    id: 'card-1',
    titulo: 'Card 1',
    colunaId: 'coluna-2',
    responsavelNome: 'Ana',
  })

  assert.equal(grupos[0].topicos.length, 2)
  assert.equal(grupos[0].topicos[0].colunaId, 'coluna-2')
  assert.equal(grupos[0].topicos[0].responsavelNome, 'Ana')
})

test('atualizarTopicoNoGrupo insere um card novo', () => {
  const grupos = atualizarTopicoNoGrupo([GRUPO_A], 'grupo-a', {
    id: 'card-3',
    titulo: 'Card 3',
    colunaId: 'coluna-1',
  })

  assert.equal(grupos[0].topicos.length, 3)
  assert.equal(grupos[0].topicos[2].id, 'card-3')
})

test('atualizarTopicoNoGrupo não altera outros quadros', () => {
  const grupos = atualizarTopicoNoGrupo([GRUPO_A, GRUPO_B], 'grupo-a', {
    id: 'card-1',
    titulo: 'Card 1',
    colunaId: 'coluna-2',
  })

  assert.equal(grupos[1], GRUPO_B)
})

test('atualizarTopicoNoGrupo é idempotente para o mesmo evento', () => {
  const evento = { id: 'card-1', titulo: 'Card 1', colunaId: 'coluna-2' }

  const umaVez = atualizarTopicoNoGrupo([GRUPO_A], 'grupo-a', evento)
  const duasVezes = atualizarTopicoNoGrupo(umaVez, 'grupo-a', evento)

  assert.equal(duasVezes[0].topicos.length, 2)
})

test('atualizarTopicoNoGrupo mantém os demais quadros quando o grupo não existe', () => {
  const grupos = atualizarTopicoNoGrupo([GRUPO_B], 'grupo-inexistente', {
    id: 'card-1',
  })

  assert.deepEqual(grupos, [GRUPO_B])
})
```

- [ ] **Step 3: Rodar e confirmar que falham**

Run: `cd meu-estudo-frontend && npm test`
Expected: FAIL — `atualizarTopicoNoGrupo is not a function` / import inválido.

- [ ] **Step 4: Implementar `atualizarTopicoNoGrupo` em `src/utils/quadro.js`**

```js
/**
 * Aplica um evento em tempo real ao estado dos quadros. Substitui o card por id
 * quando ele já existe e insere quando é novo; grupos com outro id ficam intocados.
 */
export function atualizarTopicoNoGrupo(grupos, grupoId, topico) {
  return (grupos ?? []).map((grupo) => {
    if (grupo.id !== grupoId) return grupo

    const topicos = grupo.topicos ?? []

    return {
      ...grupo,
      topicos: topicos.some((item) => item.id === topico.id)
        ? topicos.map((item) =>
            item.id === topico.id ? { ...item, ...topico } : item,
          )
        : [...topicos, topico],
    }
  })
}
```

- [ ] **Step 5: Rodar e confirmar que passam**

Run: `cd meu-estudo-frontend && npm test`
Expected: PASS — todos os testes verdes.

- [ ] **Step 6: Criar `src/services/realtimeService.js`**

```js
import * as signalR from '@microsoft/signalr'
import { API_ORIGIN, TOKEN_KEY } from './api.js'

/**
 * Abre a conexão com o hub do quadro e registra os callbacks.
 * Falhas de conexão são silenciosas: o quadro continua funcionando pelas ações normais.
 * Retorna a função que encerra a conexão.
 */
export function conectarTempoReal({
  onTopicoAtualizado,
  onReconectado,
} = {}) {
  const conexao = new signalR.HubConnectionBuilder()
    .withUrl(`${API_ORIGIN}/hubs/quadro`, {
      accessTokenFactory: () => localStorage.getItem(TOKEN_KEY) ?? '',
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build()

  conexao.on('TopicoAtualizado', (evento) => {
    onTopicoAtualizado?.(evento)
  })

  conexao.onreconnected(() => {
    onReconectado?.()
  })

  let encerrado = false

  async function iniciar() {
    try {
      await conexao.start()
    } catch {
      if (!encerrado) setTimeout(iniciar, 5000)
    }
  }

  iniciar()

  return () => {
    encerrado = true
    conexao.stop().catch(() => {})
  }
}
```

- [ ] **Step 7: Lint**

Run: `cd meu-estudo-frontend && npm run lint`
Expected: sem erros.

---

### Task 4: Ligar o tempo real no Dashboard e validar ponta a ponta

**Files:**
- Modify: `meu-estudo-frontend/src/pages/Dashboard.jsx`
- Test: verificação manual com duas sessões + `npm run build`/`npm run lint`

**Interfaces:**
- Consumes: `conectarTempoReal` e `atualizarTopicoNoGrupo` da Task 3; `recarregarDados` já existente.

- [ ] **Step 1: Importar e conectar no `Dashboard`**

Adicionar aos imports:

```js
import { conectarTempoReal } from '../services/realtimeService'
import { atualizarTopicoNoGrupo } from '../utils/quadro'
```

Depois da definição de `recarregarDados`, adicionar:

```js
useEffect(() => {
  const desconectar = conectarTempoReal({
    onTopicoAtualizado: ({ grupoId, topico }) => {
      if (!grupoId || !topico?.id) return

      setGrupos((atuais) => atualizarTopicoNoGrupo(atuais, grupoId, topico))
    },
    onReconectado: () => {
      // Eventos perdidos durante a queda de conexão são recuperados pelo refetch.
      recarregarDados().catch(() => {})
    },
  })

  return desconectar
}, [])
```

- [ ] **Step 2: Build e lint**

Run: `cd meu-estudo-frontend && npm run build && npm run lint`
Expected: build concluído sem erros; lint sem avisos.

- [ ] **Step 3: Verificação ponta a ponta**

1. Recompilar e reiniciar a API (`dotnet build` + reiniciar o processo da porta 5123).
2. Subir o frontend (`npm run dev`).
3. Abrir duas janelas anônimas distintas, logar como dono e como membro aceito do mesmo quadro acadêmico.
4. Em uma janela, alterar o responsável de um card e mover um card de coluna.
5. Na outra janela, sem recarregar, confirmar que o nome do responsável e a coluna do card mudam.
6. Fechar a API por alguns segundos e confirmar que criar/mover cards ainda mostra o erro normal de requisição (a falha do hub não deve travar a UI); religar a API e confirmar que a conexão volta e que um novo refetch sincroniza o quadro.

Expected: passos 5 e 6 confirmados.

---

## Self-review

- **Cobertura do spec:** evento por usuário, JWT na query string, quatro fluxos notificados, destinatários restritos, frontend com reconexão e upsert, refetch na reconexão — todos têm tarefa.
- **Consistência de tipos:** `IQuadroNotificador.TopicoAtualizadoAsync` usado igual nas Tasks 1 e 2; `atualizarTopicoNoGrupo(grupos, grupoId, topico)` igual nas Tasks 3 e 4; evento `{ grupoId, topico }` igual nas Tasks 2, 3 e 4.
- **Fora do escopo confirmado:** convites, presença e fila offline não têm tarefa.
