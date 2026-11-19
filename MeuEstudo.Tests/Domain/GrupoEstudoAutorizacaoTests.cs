using MeuEstudo.Domain.Entities;
using MeuEstudo.Domain.Enums;
using MeuEstudo.Domain.Exceptions;

namespace MeuEstudo.Tests.Domain;

/// <summary>
/// Testes de controle de acesso do agregado <see cref="GrupoEstudo"/>.
/// Cobrem IDOR, escalonamento de privilégio e convites aceitos por quem não foi convidado.
/// </summary>
public sealed class GrupoEstudoAutorizacaoTests
{
    private static readonly Guid DonoId = Guid.Parse("d0642713-a0e3-4fbf-a486-ded7452fa975");
    private static readonly Guid MembroId = Guid.Parse("0b5f8f03-2a84-4c8d-9f5f-5c3e1f2a7b11");
    private static readonly Guid PendenteId = Guid.Parse("9c1d0e24-7a3b-4f6e-8d21-3b4c5d6e7f80");
    private static readonly Guid EstranhoId = Guid.Parse("4d2c1b0a-9f8e-4d7c-8b6a-5f4e3d2c1b0a");

    private const string ParticipacaoNegada =
        "Apenas o dono ou membros com convite aceito podem executar esta ação.";

    private const string ApenasDono = "Apenas o dono do grupo pode executar esta ação.";

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
    public void Estranho_NaoConsegueLerNemAlterarCardDeOutroUsuario()
    {
        var grupo = CriarGrupoComMembroAceito();
        var topico = CriarCard(grupo);
        var coluna = grupo.Colunas.OrderBy(coluna => coluna.Ordem).Skip(1).First();

        Assert.Equal(
            ParticipacaoNegada,
            Assert.Throws<DomainException>(
                () => grupo.AdicionarTopico(EstranhoId, coluna.Id, "Invasão")).Message);

        Assert.Equal(
            ParticipacaoNegada,
            Assert.Throws<DomainException>(
                () => grupo.MoverTopico(EstranhoId, topico.Id, coluna.Id, 0)).Message);

        Assert.Equal(
            ParticipacaoNegada,
            Assert.Throws<DomainException>(
                () => grupo.AlternarConclusaoTopico(topico.Id, EstranhoId)).Message);

        Assert.Equal(
            ParticipacaoNegada,
            Assert.Throws<DomainException>(
                () => grupo.AlterarResponsavelTopico(EstranhoId, topico.Id, EstranhoId)).Message);
    }

    [Fact]
    public void MembroPendente_NaoTemPermissoesDeMembroAceito()
    {
        var grupo = CriarGrupo();
        grupo.ConvidarMembro(DonoId, PendenteId);
        var coluna = grupo.Colunas.OrderBy(coluna => coluna.Ordem).First();

        var excecao = Assert.Throws<DomainException>(
            () => grupo.AdicionarTopico(PendenteId, coluna.Id, "Card indevido"));

        Assert.Equal(ParticipacaoNegada, excecao.Message);
        Assert.Empty(grupo.Topicos);
    }

    [Fact]
    public void MembroAceito_PodeColaborarNoQuadro()
    {
        var grupo = CriarGrupoComMembroAceito();
        var coluna = grupo.Colunas.OrderBy(coluna => coluna.Ordem).First();

        var topico = grupo.AdicionarTopico(MembroId, coluna.Id, "Card do membro");
        grupo.MoverTopico(MembroId, topico.Id, coluna.Id, 0);
        grupo.AlterarResponsavelTopico(MembroId, topico.Id, MembroId);

        Assert.Equal(MembroId, topico.ResponsavelId);
    }

    [Fact]
    public void MembroComum_NaoPodeConvidarOutroUsuario()
    {
        var grupo = CriarGrupoComMembroAceito();

        var excecao = Assert.Throws<DomainException>(
            () => grupo.ConvidarMembro(MembroId, EstranhoId));

        Assert.Equal(ApenasDono, excecao.Message);
        Assert.DoesNotContain(grupo.Membros, membro => membro.UsuarioId == EstranhoId);
    }

    [Fact]
    public void MembroComum_NaoPodeRemoverOutroMembro()
    {
        var grupo = CriarGrupoComMembroAceito();

        var excecao = Assert.Throws<DomainException>(
            () => grupo.RemoverMembro(MembroId, PendenteId));

        Assert.Equal(ApenasDono, excecao.Message);
    }

    [Fact]
    public void Dono_NaoPodeSerRemovidoDoProprioGrupo()
    {
        var grupo = CriarGrupoComMembroAceito();

        var excecao = Assert.Throws<DomainException>(
            () => grupo.RemoverMembro(DonoId, DonoId));

        Assert.Equal("O dono do grupo não pode ser removido.", excecao.Message);
        Assert.Single(grupo.Membros);
    }

    [Fact]
    public void Usuario_NaoPodeAceitarConviteDestinadoAOutraPessoa()
    {
        var grupo = CriarGrupo();
        grupo.ConvidarMembro(DonoId, PendenteId);

        var excecao = Assert.Throws<DomainException>(
            () => grupo.ResponderConvite(EstranhoId, true));

        Assert.Equal("Nenhum convite encontrado para este usuário no grupo.", excecao.Message);
        Assert.DoesNotContain(grupo.Membros, membro => membro.UsuarioId == EstranhoId);
    }

    [Fact]
    public void Convite_NaoPodeSerRespondidoDuasVezes()
    {
        var grupo = CriarGrupo();
        grupo.ConvidarMembro(DonoId, PendenteId);
        grupo.ResponderConvite(PendenteId, false);

        var excecao = Assert.Throws<DomainException>(
            () => grupo.ResponderConvite(PendenteId, true));

        Assert.Equal("Este convite já foi respondido.", excecao.Message);
        Assert.Equal(
            StatusMembro.Recusado,
            grupo.Membros.Single(membro => membro.UsuarioId == PendenteId).Status);
    }

    [Fact]
    public void QuadroPessoal_NaoAceitaConvites()
    {
        var grupo = new GrupoEstudo("Meu quadro", null, TipoGrupo.Pessoal, DonoId);

        var excecao = Assert.Throws<DomainException>(
            () => grupo.ConvidarMembro(DonoId, MembroId));

        Assert.Equal("Quadros pessoais não permitem convidar outros membros.", excecao.Message);
    }

    [Fact]
    public void Dono_NaoPodeConvidarASiMesmo()
    {
        var grupo = CriarGrupo();

        var excecao = Assert.Throws<DomainException>(
            () => grupo.ConvidarMembro(DonoId, DonoId));

        Assert.Equal("O dono do grupo não pode convidar a si mesmo.", excecao.Message);
    }

    [Fact]
    public void NaoEhPossivelAtribuirCardAUsuarioDeOutroGrupo()
    {
        var grupo = CriarGrupo();
        var outroGrupo = CriarGrupoComMembroAceito();
        var topico = CriarCard(outroGrupo);

        var excecao = Assert.Throws<DomainException>(
            () => grupo.AlterarResponsavelTopico(DonoId, topico.Id, MembroId));

        Assert.Equal("Tópico não encontrado neste grupo.", excecao.Message);
    }
}
