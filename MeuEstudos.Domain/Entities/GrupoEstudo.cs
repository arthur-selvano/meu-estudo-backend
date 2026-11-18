using MeuEstudos.Domain.Enums;
using MeuEstudos.Domain.Exceptions;

namespace MeuEstudos.Domain.Entities;

/// <summary>
/// Raiz de agregado que representa um grupo de estudo.
/// Todas as alterações em <see cref="MembroGrupo"/> e <see cref="TopicoEstudo"/> devem passar por esta classe,
/// que concentra as regras de permissão e consistência do grupo.
/// </summary>
public class GrupoEstudo
{
    private const int TituloMaxLength = 150;

    private readonly List<MembroGrupo> _membros = new();
    private readonly List<TopicoEstudo> _topicos = new();

    public Guid Id { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public Guid DonoId { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public IReadOnlyCollection<MembroGrupo> Membros => _membros.AsReadOnly();
    public IReadOnlyCollection<TopicoEstudo> Topicos => _topicos.AsReadOnly();

    /// <summary>
    /// Construtor sem parâmetros exigido pelo ORM (EF Core). Não deve ser usado pela aplicação.
    /// </summary>
    protected GrupoEstudo()
    {
    }

    public GrupoEstudo(string titulo, string? descricao, Guid donoId)
    {
        if (donoId == Guid.Empty)
            throw new DomainException("O dono do grupo é obrigatório.");

        Id = Guid.NewGuid();
        DefinirTitulo(titulo);
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        DonoId = donoId;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Altera título e descrição do grupo. Permitido apenas para o dono.
    /// </summary>
    public void AlterarDetalhes(Guid solicitanteId, string titulo, string? descricao)
    {
        GarantirQueEhDono(solicitanteId);

        DefinirTitulo(titulo);
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
    }

    /// <summary>
    /// Convida um usuário, assumindo que o solicitante é o dono do grupo.
    /// A camada de aplicação deve garantir que o usuário autenticado é o dono antes de chamar este método;
    /// ao usar <see cref="ConvidarMembro(Guid, Guid)"/>, essa verificação é feita no próprio domínio.
    /// </summary>
    /// <param name="usuarioId">Usuário que receberá o convite.</param>
    /// <returns>O vínculo pendente criado.</returns>
    public MembroGrupo ConvidarMembro(Guid usuarioId) => ConvidarMembro(DonoId, usuarioId);

    /// <summary>
    /// Convida um usuário para o grupo. Apenas o dono pode convidar, não é possível convidar a si mesmo
    /// nem criar um segundo vínculo para alguém que já foi convidado (independentemente do status).
    /// </summary>
    /// <param name="solicitanteId">Usuário que está executando a ação; precisa ser o dono do grupo.</param>
    /// <param name="usuarioId">Usuário que receberá o convite.</param>
    /// <returns>O vínculo pendente criado.</returns>
    public MembroGrupo ConvidarMembro(Guid solicitanteId, Guid usuarioId)
    {
        GarantirQueEhDono(solicitanteId);

        if (usuarioId == Guid.Empty)
            throw new DomainException("O usuário a ser convidado é obrigatório.");

        if (usuarioId == DonoId)
            throw new DomainException("O dono do grupo não pode convidar a si mesmo.");

        if (_membros.Any(m => m.UsuarioId == usuarioId))
            throw new DomainException("Este usuário já possui um convite ou já é membro do grupo.");

        var membro = new MembroGrupo(Id, usuarioId);
        _membros.Add(membro);

        return membro;
    }

    /// <summary>
    /// Responde ao convite do usuário, aceitando ou recusando a participação e registrando a data da resposta.
    /// </summary>
    /// <param name="usuarioId">Usuário convidado.</param>
    /// <param name="aceitar">Verdadeiro para aceitar; falso para recusar.</param>
    public void ResponderConvite(Guid usuarioId, bool aceitar)
    {
        var membro = _membros.SingleOrDefault(m => m.UsuarioId == usuarioId)
                     ?? throw new DomainException("Nenhum convite encontrado para este usuário no grupo.");

        if (membro.Status != StatusMembro.Pendente)
            throw new DomainException("Este convite já foi respondido.");

        membro.Responder(aceitar);
    }

    /// <summary>
    /// Adiciona um tópico de estudo ao grupo. Permitido apenas para o dono ou para membros com convite aceito.
    /// </summary>
    public TopicoEstudo AdicionarTopico(
        Guid usuarioId,
        string titulo,
        string? descricao = null,
        DateOnly? dataPrevista = null)
    {
        GarantirParticipacao(usuarioId);

        var topico = new TopicoEstudo(Id, usuarioId, titulo, descricao, dataPrevista);
        _topicos.Add(topico);

        return topico;
    }

    /// <summary>
    /// Inverte a conclusão de um tópico. Permitido apenas para o dono ou para membros com convite aceito.
    /// </summary>
    public TopicoEstudo AlternarConclusaoTopico(Guid topicoId, Guid usuarioId)
    {
        GarantirParticipacao(usuarioId);

        var topico = _topicos.SingleOrDefault(t => t.Id == topicoId)
                     ?? throw new DomainException("Tópico não encontrado neste grupo.");

        topico.AlternarConclusao();

        return topico;
    }

    private void DefinirTitulo(string titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new DomainException("O título do grupo é obrigatório.");

        var tituloNormalizado = titulo.Trim();

        if (tituloNormalizado.Length > TituloMaxLength)
            throw new DomainException($"O título do grupo deve ter no máximo {TituloMaxLength} caracteres.");

        Titulo = tituloNormalizado;
    }

    /// <summary>
    /// Garante que o solicitante é o dono do grupo. Exposto para que a camada de
    /// aplicação possa anteceder validações que não devem rodar para não-donos.
    /// </summary>
    public void GarantirQueEhDono(Guid usuarioId)
    {
        if (usuarioId != DonoId)
            throw new DomainException("Apenas o dono do grupo pode executar esta ação.");
    }

    private void GarantirParticipacao(Guid usuarioId)
    {
        if (usuarioId == DonoId)
            return;

        var membro = _membros.SingleOrDefault(m => m.UsuarioId == usuarioId);

        if (membro is null || !membro.EhMembroAceito())
            throw new DomainException("Apenas o dono ou membros com convite aceito podem executar esta ação.");
    }
}
