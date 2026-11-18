using MeuEstudos.Domain.Enums;
using MeuEstudos.Domain.Exceptions;

namespace MeuEstudos.Domain.Entities;

/// <summary>
/// Vínculo entre um usuário e um grupo de estudo, controlando o convite e o seu status.
/// Entidade interna do agregado <see cref="GrupoEstudo"/>: não deve ser criada ou alterada fora dele.
/// </summary>
public class MembroGrupo
{
    public Guid Id { get; private set; }
    public Guid GrupoId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public StatusMembro Status { get; private set; }
    public DateTime ConvidadoEm { get; private set; }
    public DateTime? RespondidoEm { get; private set; }

    /// <summary>
    /// Construtor sem parâmetros exigido pelo ORM (EF Core). Não deve ser usado pela aplicação.
    /// </summary>
    protected MembroGrupo()
    {
    }

    internal MembroGrupo(Guid grupoId, Guid usuarioId)
    {
        if (grupoId == Guid.Empty)
            throw new DomainException("O grupo do membro é obrigatório.");

        if (usuarioId == Guid.Empty)
            throw new DomainException("O usuário do membro é obrigatório.");

        Id = Guid.NewGuid();
        GrupoId = grupoId;
        UsuarioId = usuarioId;
        Status = StatusMembro.Pendente;
        ConvidadoEm = DateTime.UtcNow;
    }

    internal void Responder(bool aceitar)
    {
        if (Status != StatusMembro.Pendente)
            throw new DomainException("Este convite já foi respondido.");

        Status = aceitar ? StatusMembro.Aceito : StatusMembro.Recusado;
        RespondidoEm = DateTime.UtcNow;
    }

    public bool EhMembroAceito() => Status == StatusMembro.Aceito;
}
