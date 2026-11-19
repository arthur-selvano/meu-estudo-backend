using MeuEstudo.Application.DTOs;
using MeuEstudo.Application.Interfaces;
using MeuEstudo.Domain.Entities;
using MeuEstudo.Domain.Enums;
using MeuEstudo.Domain.Exceptions;
using MeuEstudo.Domain.Repositories;

namespace MeuEstudo.Application.Services;

public sealed class GrupoEstudoService : IGrupoEstudoService
{
    private readonly IGrupoEstudoRepository _grupoEstudoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IQuadroNotificador _quadroNotificador;

    public GrupoEstudoService(
        IGrupoEstudoRepository grupoEstudoRepository,
        IUsuarioRepository usuarioRepository,
        IQuadroNotificador quadroNotificador)
    {
        _grupoEstudoRepository = grupoEstudoRepository
            ?? throw new ArgumentNullException(nameof(grupoEstudoRepository));
        _usuarioRepository = usuarioRepository
            ?? throw new ArgumentNullException(nameof(usuarioRepository));
        _quadroNotificador = quadroNotificador
            ?? throw new ArgumentNullException(nameof(quadroNotificador));
    }

    public async Task<GrupoEstudoResponse> CriarAsync(
        Guid usuarioId,
        CriarGrupoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tipo = ParseTipoGrupo(request.Tipo);
        var grupo = new GrupoEstudo(request.Titulo, request.Descricao, tipo, usuarioId);

        await _grupoEstudoRepository.AdicionarAsync(grupo, cancellationToken);

        var usuarios = await ObterResumosAsync(new[] { grupo.DonoId }, cancellationToken);

        return MapearGrupo(grupo, usuarios, usuarioId);
    }

    public async Task<IReadOnlyCollection<GrupoEstudoResponse>> ListarDoUsuarioAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var grupos = await _grupoEstudoRepository.ObterDoUsuarioAsync(usuarioId, cancellationToken);

        var usuarios = await ObterResumosAsync(
            grupos.SelectMany(grupo => UsuariosRelevantes(grupo, usuarioId)),
            cancellationToken);

        return grupos.Select(grupo => MapearGrupo(grupo, usuarios, usuarioId)).ToList();
    }

    public async Task<GrupoEstudoResponse> ObterDetalhesAsync(
        Guid usuarioId,
        Guid grupoId,
        CancellationToken cancellationToken = default)
    {
        var grupo = await ObterGrupoAsync(grupoId, cancellationToken);

        grupo.GarantirParticipacao(usuarioId);

        var usuarios = await ObterResumosAsync(
            UsuariosRelevantes(grupo, usuarioId),
            cancellationToken);

        return MapearGrupo(grupo, usuarios, usuarioId);
    }

    public async Task<IReadOnlyCollection<ConvitePendenteResponse>> ListarConvitesPendentesAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var grupos = await _grupoEstudoRepository.ObterComConvitePendenteAsync(usuarioId, cancellationToken);

        var nomes = await _usuarioRepository.ObterNomesPorIdsAsync(
            grupos.Select(grupo => grupo.DonoId),
            cancellationToken);

        return grupos
            .Select(grupo =>
            {
                var convite = grupo.Membros.Single(
                    m => m.UsuarioId == usuarioId && m.Status == StatusMembro.Pendente);

                return new ConvitePendenteResponse(
                    grupo.Id,
                    grupo.Titulo,
                    grupo.DonoId,
                    nomes.GetValueOrDefault(grupo.DonoId, string.Empty),
                    convite.ConvidadoEm);
            })
            .ToList();
    }

    public async Task ConvidarMembroAsync(
        Guid usuarioId,
        Guid grupoId,
        ConvidarMembroRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new DomainException("O e-mail do usuário a ser convidado é obrigatório.");

        var grupo = await ObterGrupoAsync(grupoId, cancellationToken);

        // Antes de consultar o e-mail, garante que o solicitante é o dono: assim um
        // não-dono não consegue descobrir se um e-mail está cadastrado pela mensagem.
        grupo.GarantirQueEhDono(usuarioId);
        grupo.GarantirConvitePermitido();

        var convidado = await _usuarioRepository.ObterPorEmailAsync(request.Email, cancellationToken)
            ?? throw new DomainException("Usuário não encontrado para o e-mail informado.");

        // O overload sem solicitante assume que a checagem de dono já foi feita acima;
        // o próprio domínio também bloqueia convites em quadros pessoais.
        grupo.ConvidarMembro(convidado.Id);

        await _grupoEstudoRepository.AtualizarAsync(grupo, cancellationToken);
    }

    public async Task ResponderConviteAsync(
        Guid usuarioId,
        Guid grupoId,
        ResponderConviteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var grupo = await ObterGrupoAsync(grupoId, cancellationToken);

        grupo.ResponderConvite(usuarioId, request.Aceitar);

        await _grupoEstudoRepository.AtualizarAsync(grupo, cancellationToken);
    }

    public async Task<TopicoEstudoResponse> AdicionarTopicoAsync(
        Guid usuarioId,
        Guid grupoId,
        CriarTopicoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prioridade = ParsePrioridade(request.Prioridade);
        var grupo = await ObterGrupoAsync(grupoId, cancellationToken);

        var topico = grupo.AdicionarTopico(
            usuarioId,
            request.ColunaId,
            request.Titulo,
            request.Descricao,
            request.ResponsavelId,
            prioridade,
            request.DataPrevista);

        await _grupoEstudoRepository.AtualizarAsync(grupo, cancellationToken);

        var usuarios = await ObterResumosDoResponsavelAsync(topico, cancellationToken);
        var resposta = MapearTopico(topico, usuarios);

        await _quadroNotificador.TopicoAtualizadoAsync(
            grupo.Id,
            DestinatariosDoQuadro(grupo),
            resposta,
            cancellationToken);

        return resposta;
    }

    public async Task<TopicoEstudoResponse> AlternarConclusaoTopicoAsync(
        Guid usuarioId,
        Guid grupoId,
        Guid topicoId,
        CancellationToken cancellationToken = default)
    {
        var grupo = await ObterGrupoAsync(grupoId, cancellationToken);

        var topico = grupo.AlternarConclusaoTopico(topicoId, usuarioId);

        await _grupoEstudoRepository.AtualizarAsync(grupo, cancellationToken);

        var usuarios = await ObterResumosDoResponsavelAsync(topico, cancellationToken);
        var resposta = MapearTopico(topico, usuarios);

        await _quadroNotificador.TopicoAtualizadoAsync(
            grupo.Id,
            DestinatariosDoQuadro(grupo),
            resposta,
            cancellationToken);

        return resposta;
    }

    public async Task<TopicoEstudoResponse> MoverTopicoAsync(
        Guid usuarioId,
        Guid topicoId,
        MoverTopicoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var grupo = await _grupoEstudoRepository.ObterPorTopicoIdAsync(topicoId, cancellationToken)
                    ?? throw new KeyNotFoundException("Tópico não encontrado.");

        var topico = grupo.MoverTopico(usuarioId, topicoId, request.ColunaId, request.Ordem);

        await _grupoEstudoRepository.AtualizarAsync(grupo, cancellationToken);

        var usuarios = await ObterResumosDoResponsavelAsync(topico, cancellationToken);
        var resposta = MapearTopico(topico, usuarios);

        await _quadroNotificador.TopicoAtualizadoAsync(
            grupo.Id,
            DestinatariosDoQuadro(grupo),
            resposta,
            cancellationToken);

        return resposta;
    }

    public async Task<TopicoEstudoResponse> AlterarResponsavelTopicoAsync(
        Guid usuarioId,
        Guid topicoId,
        AlterarResponsavelTopicoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var grupo = await _grupoEstudoRepository.ObterPorTopicoIdAsync(topicoId, cancellationToken)
                    ?? throw new KeyNotFoundException("Tópico não encontrado.");

        var topico = grupo.AlterarResponsavelTopico(usuarioId, topicoId, request.ResponsavelId);

        await _grupoEstudoRepository.AtualizarAsync(grupo, cancellationToken);

        var usuarios = await ObterResumosDoResponsavelAsync(topico, cancellationToken);
        var resposta = MapearTopico(topico, usuarios);

        await _quadroNotificador.TopicoAtualizadoAsync(
            grupo.Id,
            DestinatariosDoQuadro(grupo),
            resposta,
            cancellationToken);

        return resposta;
    }

    public async Task RemoverMembroAsync(
        Guid usuarioId,
        Guid grupoId,
        Guid membroId,
        CancellationToken cancellationToken = default)
    {
        var grupo = await ObterGrupoAsync(grupoId, cancellationToken);

        grupo.RemoverMembro(usuarioId, membroId);

        await _grupoEstudoRepository.AtualizarAsync(grupo, cancellationToken);
    }

    public async Task ExcluirGrupoAsync(
        Guid usuarioId,
        Guid grupoId,
        CancellationToken cancellationToken = default)
    {
        var grupo = await ObterGrupoAsync(grupoId, cancellationToken);

        grupo.GarantirQueEhDono(usuarioId);

        await _grupoEstudoRepository.RemoverAsync(grupo, cancellationToken);
    }

    private static IReadOnlyCollection<Guid> DestinatariosDoQuadro(GrupoEstudo grupo)
        => grupo.Membros
            .Where(membro => membro.EhMembroAceito())
            .Select(membro => membro.UsuarioId)
            .Append(grupo.DonoId)
            .Distinct()
            .ToList();

    private async Task<GrupoEstudo> ObterGrupoAsync(Guid grupoId, CancellationToken cancellationToken)
        => await _grupoEstudoRepository.ObterPorIdAsync(grupoId, cancellationToken)
           ?? throw new KeyNotFoundException("Grupo de estudo não encontrado.");

    private Task<IReadOnlyDictionary<Guid, UsuarioResumo>> ObterResumosDoResponsavelAsync(
        TopicoEstudo topico,
        CancellationToken cancellationToken)
        => topico.ResponsavelId is Guid responsavelId
            ? _usuarioRepository.ObterResumosPorIdsAsync(new[] { responsavelId }, cancellationToken)
            : Task.FromResult<IReadOnlyDictionary<Guid, UsuarioResumo>>(
                new Dictionary<Guid, UsuarioResumo>());

    private Task<IReadOnlyDictionary<Guid, UsuarioResumo>> ObterResumosAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken)
        => _usuarioRepository.ObterResumosPorIdsAsync(ids, cancellationToken);

    private static IEnumerable<MembroGrupo> MembrosVisiveis(GrupoEstudo grupo, Guid solicitanteId)
        => grupo.Membros.Where(membro =>
            solicitanteId == grupo.DonoId || membro.EhMembroAceito());

    /// <summary>
    /// Usuários cujos dados precisam aparecer na resposta: membros visíveis, dono e responsáveis pelos cards.
    /// </summary>
    private static IEnumerable<Guid> UsuariosRelevantes(GrupoEstudo grupo, Guid solicitanteId)
        => MembrosVisiveis(grupo, solicitanteId)
            .Select(membro => membro.UsuarioId)
            .Append(grupo.DonoId)
            .Concat(grupo.Topicos
                .Where(topico => topico.ResponsavelId is not null)
                .Select(topico => topico.ResponsavelId!.Value));

    private static GrupoEstudoResponse MapearGrupo(
        GrupoEstudo grupo,
        IReadOnlyDictionary<Guid, UsuarioResumo> usuarios,
        Guid solicitanteId)
    {
        var membros = MembrosVisiveis(grupo, solicitanteId)
            .Select(membro => new MembroGrupoResponse(
                membro.UsuarioId,
                ObterNome(usuarios, membro.UsuarioId),
                membro.Status.ToString()))
            .ToList();

        var topicosPorColuna = grupo.Topicos
            .GroupBy(topico => topico.ColunaId)
            .ToDictionary(
                grupoColuna => grupoColuna.Key,
                grupoColuna => (IReadOnlyCollection<TopicoEstudoResponse>)grupoColuna
                    .OrderBy(topico => topico.Ordem)
                    .Select(topico => MapearTopico(topico, usuarios))
                    .ToList());

        var colunas = grupo.Colunas
            .OrderBy(coluna => coluna.Ordem)
            .Select(coluna => new ColunaKanbanResponse(
                coluna.Id,
                coluna.Titulo,
                coluna.Ordem,
                topicosPorColuna.TryGetValue(coluna.Id, out var topicosDaColuna)
                    ? topicosDaColuna
                    : Array.Empty<TopicoEstudoResponse>()))
            .ToList();

        // A lista plana mantém a mesma ordenação do Kanban: coluna e, dentro dela, posição.
        var topicos = colunas.SelectMany(coluna => coluna.Topicos).ToList();

        return new(
            grupo.Id,
            grupo.Titulo,
            grupo.Descricao,
            grupo.Tipo.ParaValorBanco(),
            grupo.DonoId,
            ObterNome(usuarios, grupo.DonoId),
            grupo.CriadoEm,
            membros,
            topicos,
            colunas);
    }

    private static TopicoEstudoResponse MapearTopico(
        TopicoEstudo topico,
        IReadOnlyDictionary<Guid, UsuarioResumo> usuarios)
    {
        UsuarioResumo? responsavel = null;

        if (topico.ResponsavelId is Guid responsavelId)
            usuarios.TryGetValue(responsavelId, out responsavel);

        return new(
            topico.Id,
            topico.Titulo,
            topico.Descricao,
            topico.ColunaId,
            topico.ResponsavelId,
            responsavel?.Nome,
            responsavel?.FotoUrl,
            topico.Prioridade.ParaValorBanco(),
            topico.Ordem,
            topico.DataPrevista,
            topico.Concluido,
            topico.CriadoPorId,
            topico.CriadoEm);
    }

    private static string ObterNome(IReadOnlyDictionary<Guid, UsuarioResumo> usuarios, Guid usuarioId)
        => usuarios.TryGetValue(usuarioId, out var usuario) ? usuario.Nome : string.Empty;

    private static TipoGrupo ParseTipoGrupo(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            return TipoGrupo.TrabalhoAcademico;

        return TipoGrupoExtensions.TentarConverter(tipo, out var valor)
            ? valor
            : throw new DomainException("O tipo do grupo deve ser PESSOAL ou TRABALHO_ACADEMICO.");
    }

    private static PrioridadeTopico ParsePrioridade(string? prioridade)
    {
        if (string.IsNullOrWhiteSpace(prioridade))
            return PrioridadeTopico.Media;

        return PrioridadeTopicoExtensions.TentarConverter(prioridade, out var valor)
            ? valor
            : throw new DomainException("A prioridade do tópico deve ser BAIXA, MEDIA ou ALTA.");
    }
}
