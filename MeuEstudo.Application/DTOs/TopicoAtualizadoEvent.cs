namespace MeuEstudo.Application.DTOs;

/// <summary>Evento enviado em tempo real quando um card do quadro é criado ou alterado.</summary>
public sealed record TopicoAtualizadoEvent(Guid GrupoId, TopicoEstudoResponse Topico);
