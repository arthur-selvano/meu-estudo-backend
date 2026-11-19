using MeuEstudo.Domain.Enums;
using MeuEstudo.Domain.Exceptions;

namespace MeuEstudo.Domain.Entities;

/// <summary>
/// Raiz de agregado que representa um quadro de estudos (pessoal ou acadêmico).
/// Todas as alterações em <see cref="MembroGrupo"/>, <see cref="ColunaKanban"/> e <see cref="TopicoEstudo"/>
/// devem passar por esta classe, que concentra as regras de permissão e consistência do grupo.
/// </summary>
public class GrupoEstudo
{
    private const int TituloMaxLength = 150;

    /// <summary>
    /// Colunas criadas automaticamente em todo quadro, na ordem em que aparecem no Kanban.
    /// A última coluna representa a conclusão do card.
    /// </summary>
    private static readonly (string Titulo, int Ordem)[] ColunasPadrao =
    [
        ("A Fazer", 0),
        ("Em Andamento", 1),
        ("Revisão", 2),
        ("Concluído", 3)
    ];

    private readonly List<MembroGrupo> _membros = new();
    private readonly List<ColunaKanban> _colunas = new();
    private readonly List<TopicoEstudo> _topicos = new();

    public Guid Id { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public TipoGrupo Tipo { get; private set; }
    public Guid DonoId { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public IReadOnlyCollection<MembroGrupo> Membros => _membros.AsReadOnly();
    public IReadOnlyCollection<ColunaKanban> Colunas => _colunas.AsReadOnly();
    public IReadOnlyCollection<TopicoEstudo> Topicos => _topicos.AsReadOnly();

    /// <summary>
    /// Construtor sem parâmetros exigido pelo ORM (EF Core). Não deve ser usado pela aplicação.
    /// </summary>
    protected GrupoEstudo()
    {
    }

    public GrupoEstudo(string titulo, string? descricao, TipoGrupo tipo, Guid donoId)
    {
        if (donoId == Guid.Empty)
            throw new DomainException("O dono do grupo é obrigatório.");

        if (!Enum.IsDefined(tipo))
            throw new DomainException("O tipo do grupo é inválido.");

        Id = Guid.NewGuid();
        DefinirTitulo(titulo);
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        Tipo = tipo;
        DonoId = donoId;
        CriadoEm = DateTime.UtcNow;

        CriarColunasPadrao();
    }

    /// <summary>
    /// Alterar título e descrição do grupo. Permitido apenas para o dono.
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
    /// Convida um usuário para o grupo. Apenas o dono pode convidar, quadros pessoais não aceitam
    /// convidados, não é possível convidar a si mesmo nem criar um segundo vínculo para alguém
    /// que já foi convidado (independentemente do status).
    /// </summary>
    /// <param name="solicitanteId">Usuário que está executando a ação; precisa ser o dono do grupo.</param>
    /// <param name="usuarioId">Usuário que receberá o convite.</param>
    /// <returns>O vínculo pendente criado.</returns>
    public MembroGrupo ConvidarMembro(Guid solicitanteId, Guid usuarioId)
    {
        GarantirQueEhDono(solicitanteId);
        GarantirConvitePermitido();

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
    /// Remove um membro (ou um convite pendente) do grupo. Apenas o dono pode remover.
    /// Cards atribuídos à pessoa ficam sem responsável para manter o quadro consistente.
    /// </summary>
    /// <param name="solicitanteId">Usuário que está executando a ação; precisa ser o dono do grupo.</param>
    /// <param name="usuarioId">Usuário cujo vínculo será removido.</param>
    public void RemoverMembro(Guid solicitanteId, Guid usuarioId)
    {
        GarantirQueEhDono(solicitanteId);

        if (usuarioId == Guid.Empty)
            throw new DomainException("O membro a ser removido é obrigatório.");

        if (usuarioId == DonoId)
            throw new DomainException("O dono do grupo não pode ser removido.");

        var membro = _membros.SingleOrDefault(m => m.UsuarioId == usuarioId)
                     ?? throw new DomainException("Membro não encontrado neste grupo.");

        _membros.Remove(membro);

        foreach (var topico in _topicos.Where(t => t.ResponsavelId == usuarioId))
            topico.DefinirResponsavel(null);
    }

    /// <summary>
    /// Adiciona um card ao quadro, na coluna informada e no fim da fila dessa coluna.
    /// Permitido apenas para o dono ou para membros com convite aceito.
    /// O responsável, quando informado, também deve ser o dono ou um membro aceito.
    /// </summary>
    public TopicoEstudo AdicionarTopico(
        Guid usuarioId,
        Guid colunaId,
        string titulo,
        string? descricao = null,
        Guid? responsavelId = null,
        PrioridadeTopico prioridade = PrioridadeTopico.Media,
        DateOnly? dataPrevista = null)
    {
        GarantirParticipacao(usuarioId);

        if (colunaId == Guid.Empty)
            throw new DomainException("A coluna do card é obrigatória.");

        var coluna = _colunas.SingleOrDefault(c => c.Id == colunaId)
                     ?? throw new DomainException("Coluna não encontrada neste grupo.");

        GarantirResponsavelValido(responsavelId);

        var ordem = _topicos
            .Where(t => t.ColunaId == coluna.Id)
            .Select(t => t.Ordem)
            .DefaultIfEmpty(-1)
            .Max() + 1;

        var ultimaColuna = _colunas.Max(c => c.Ordem);

        var topico = new TopicoEstudo(
            Id,
            coluna.Id,
            usuarioId,
            titulo,
            descricao,
            responsavelId,
            prioridade,
            ordem,
            dataPrevista,
            concluido: coluna.Ordem == ultimaColuna);

        _topicos.Add(topico);

        return topico;
    }

    /// <summary>
    /// Define o responsável por um card, ou remove a atribuição quando <paramref name="responsavelId"/>
    /// é nulo. O responsável deve ser o dono ou um membro com convite aceito.
    /// Permitido apenas para o dono ou para membros com convite aceito.
    /// </summary>
    public TopicoEstudo AlterarResponsavelTopico(Guid usuarioId, Guid topicoId, Guid? responsavelId)
    {
        GarantirParticipacao(usuarioId);

        var topico = ObterTopico(topicoId);

        GarantirResponsavelValido(responsavelId);

        topico.DefinirResponsavel(responsavelId);

        return topico;
    }

    /// <summary>
    /// Inverte a conclusão de um tópico. Permitido apenas para o dono ou para membros com convite aceito.
    /// </summary>
    public TopicoEstudo AlternarConclusaoTopico(Guid topicoId, Guid usuarioId)
    {
        GarantirParticipacao(usuarioId);

        var topico = ObterTopico(topicoId);

        topico.AlternarConclusao();

        return topico;
    }

    /// <summary>
    /// Move um card para a coluna e a posição informadas. O card é marcado como concluído
    /// quando a coluna de destino é a última do quadro (e desmarcado ao sair dela).
    /// Permitido apenas para o dono ou para membros com convite aceito.
    /// </summary>
    public TopicoEstudo MoverTopico(Guid usuarioId, Guid topicoId, Guid colunaId, int ordem)
    {
        GarantirParticipacao(usuarioId);

        var topico = ObterTopico(topicoId);

        if (colunaId == Guid.Empty)
            throw new DomainException("A coluna de destino é obrigatória.");

        var colunaDestino = _colunas.SingleOrDefault(c => c.Id == colunaId)
                            ?? throw new DomainException("Coluna de destino não encontrada neste grupo.");

        var ultimaColuna = _colunas.Max(c => c.Ordem);

        topico.MoverPara(colunaDestino.Id, ordem, concluido: colunaDestino.Ordem == ultimaColuna);

        return topico;
    }

    private TopicoEstudo ObterTopico(Guid topicoId)
        => _topicos.SingleOrDefault(t => t.Id == topicoId)
           ?? throw new DomainException("Tópico não encontrado neste grupo.");

    private void CriarColunasPadrao()
    {
        foreach (var (titulo, ordem) in ColunasPadrao)
            _colunas.Add(new ColunaKanban(Id, titulo, ordem));
    }

    private void GarantirResponsavelValido(Guid? responsavelId)
    {
        if (responsavelId is null)
            return;

        if (responsavelId == Guid.Empty)
            throw new DomainException("O responsável informado é inválido.");

        if (responsavelId == DonoId)
            return;

        var membro = _membros.SingleOrDefault(m => m.UsuarioId == responsavelId);

        if (membro is null || !membro.EhMembroAceito())
            throw new DomainException("O responsável deve ser o dono ou um membro com convite aceito do grupo.");
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

    /// <summary>
    /// Garante que o tipo do grupo aceita convidados. Quadros pessoais são exclusivos do dono.
    /// Exposto para que a aplicação rejeite o convite antes de consultar o e-mail do convidado.
    /// </summary>
    public void GarantirConvitePermitido()
    {
        if (Tipo == TipoGrupo.Pessoal)
            throw new DomainException("Quadros pessoais não permitem convidar outros membros.");
    }

    /// <summary>
    /// Garante que o solicitante é o dono ou um membro com convite aceito.
    /// </summary>
    public void GarantirParticipacao(Guid usuarioId)
    {
        if (usuarioId == DonoId)
            return;

        var membro = _membros.SingleOrDefault(m => m.UsuarioId == usuarioId);

        if (membro is null || !membro.EhMembroAceito())
            throw new DomainException("Apenas o dono ou membros com convite aceito podem executar esta ação.");
    }
}
