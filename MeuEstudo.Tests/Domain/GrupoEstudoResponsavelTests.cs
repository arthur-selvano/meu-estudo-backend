using MeuEstudo.Domain.Entities;
using MeuEstudo.Domain.Enums;
using MeuEstudo.Domain.Exceptions;

namespace MeuEstudo.Tests.Domain;

public sealed class GrupoEstudoResponsavelTests
{
    private static readonly Guid DonoId = Guid.Parse("d0642713-a0e3-4fbf-a486-ded7452fa975");
    private static readonly Guid MembroId = Guid.Parse("0b5f8f03-2a84-4c8d-9f5f-5c3e1f2a7b11");
    private static readonly Guid PendenteId = Guid.Parse("9c1d0e24-7a3b-4f6e-8d21-3b4c5d6e7f80");
    private static readonly Guid EstranhoId = Guid.Parse("4d2c1b0a-9f8e-4d7c-8b6a-5f4e3d2c1b0a");

    private static GrupoEstudo CriarGrupo()
        => new("Grupo", null, TipoGrupo.TrabalhoAcademico, DonoId);

    private static GrupoEstudo CriarGrupoComMembroAceito()
    {
        var grupo = CriarGrupo();
        grupo.ConvidarMembro(DonoId, MembroId);
        grupo.ResponderConvite(MembroId, true);
        return grupo;
    }

    private static TopicoEstudo CriarCard(GrupoEstudo grupo)
        => grupo.AdicionarTopico(
            DonoId,
            grupo.Colunas.OrderBy(coluna => coluna.Ordem).First().Id,
            "Card");

    [Fact]
    public void AlterarResponsavel_AtribuiMembroAceito()
    {
        var grupo = CriarGrupoComMembroAceito();
        var topico = CriarCard(grupo);

        var alterado = grupo.AlterarResponsavelTopico(DonoId, topico.Id, MembroId);

        Assert.Equal(MembroId, alterado.ResponsavelId);
        Assert.Same(topico, alterado);
    }

    [Fact]
    public void AlterarResponsavel_AtribuiODono()
    {
        var grupo = CriarGrupo();
        var topico = CriarCard(grupo);

        grupo.AlterarResponsavelTopico(DonoId, topico.Id, DonoId);

        Assert.Equal(DonoId, topico.ResponsavelId);
    }

    [Fact]
    public void AlterarResponsavel_RemoveQuandoRecebeNull()
    {
        var grupo = CriarGrupoComMembroAceito();
        var topico = CriarCard(grupo);
        grupo.AlterarResponsavelTopico(DonoId, topico.Id, MembroId);

        grupo.AlterarResponsavelTopico(DonoId, topico.Id, null);

        Assert.Null(topico.ResponsavelId);
    }

    [Fact]
    public void AlterarResponsavel_RejeitaUsuarioComConvitePendente()
    {
        var grupo = CriarGrupo();
        grupo.ConvidarMembro(DonoId, PendenteId);
        var topico = CriarCard(grupo);

        var excecao = Assert.Throws<DomainException>(
            () => grupo.AlterarResponsavelTopico(DonoId, topico.Id, PendenteId));

        Assert.Equal(
            "O responsável deve ser o dono ou um membro com convite aceito do grupo.",
            excecao.Message);
        Assert.Null(topico.ResponsavelId);
    }

    [Fact]
    public void AlterarResponsavel_RejeitaUsuarioQueNaoParticipaDoGrupo()
    {
        var grupo = CriarGrupo();
        var topico = CriarCard(grupo);

        var excecao = Assert.Throws<DomainException>(
            () => grupo.AlterarResponsavelTopico(DonoId, topico.Id, EstranhoId));

        Assert.Equal(
            "O responsável deve ser o dono ou um membro com convite aceito do grupo.",
            excecao.Message);
        Assert.Null(topico.ResponsavelId);
    }

    [Fact]
    public void AlterarResponsavel_RejeitaGuidVazio()
    {
        var grupo = CriarGrupo();
        var topico = CriarCard(grupo);

        var excecao = Assert.Throws<DomainException>(
            () => grupo.AlterarResponsavelTopico(DonoId, topico.Id, Guid.Empty));

        Assert.Equal("O responsável informado é inválido.", excecao.Message);
        Assert.Null(topico.ResponsavelId);
    }

    [Fact]
    public void AlterarResponsavel_RejeitaTopicoInexistente()
    {
        var grupo = CriarGrupo();

        var excecao = Assert.Throws<DomainException>(
            () => grupo.AlterarResponsavelTopico(DonoId, Guid.NewGuid(), DonoId));

        Assert.Equal("Tópico não encontrado neste grupo.", excecao.Message);
    }

    [Fact]
    public void AlterarResponsavel_ExigeParticipacaoDoSolicitante()
    {
        var grupo = CriarGrupoComMembroAceito();
        var topico = CriarCard(grupo);

        var excecao = Assert.Throws<DomainException>(
            () => grupo.AlterarResponsavelTopico(EstranhoId, topico.Id, MembroId));

        Assert.Equal(
            "Apenas o dono ou membros com convite aceito podem executar esta ação.",
            excecao.Message);
    }
}
