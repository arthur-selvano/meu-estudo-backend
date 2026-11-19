using MeuEstudo.Application.DTOs;
using MeuEstudo.Application.Services;
using MeuEstudo.Domain.Entities;
using MeuEstudo.Domain.Enums;
using MeuEstudo.Domain.Exceptions;
using MeuEstudo.Tests.Fakes;

namespace MeuEstudo.Tests.Application;

/// <summary>
/// Garante que a camada de aplicação não confia em IDs enviados pelo cliente e
/// valida a relação entre o usuário autenticado e o recurso solicitado.
/// </summary>
public sealed class GrupoEstudoServiceAutorizacaoTests
{
    private static readonly Guid DonoId = Guid.Parse("d0642713-a0e3-4fbf-a486-ded7452fa975");
    private static readonly Guid MembroId = Guid.Parse("0b5f8f03-2a84-4c8d-9f5f-5c3e1f2a7b11");
    private static readonly Guid PendenteId = Guid.Parse("9c1d0e24-7a3b-4f6e-8d21-3b4c5d6e7f80");
    private static readonly Guid EstranhoId = Guid.Parse("4d2c1b0a-9f8e-4d7c-8b6a-5f4e3d2c1b0a");

    private static (GrupoEstudo Grupo, GrupoEstudoService Servico, FakeGrupoEstudoRepository Repositorio) CriarCenario()
    {
        var grupo = new GrupoEstudo("Grupo privado", null, TipoGrupo.TrabalhoAcademico, DonoId);
        grupo.ConvidarMembro(DonoId, MembroId);
        grupo.ResponderConvite(MembroId, true);
        grupo.ConvidarMembro(DonoId, PendenteId);

        var repositorio = new FakeGrupoEstudoRepository(grupo);
        var servico = new GrupoEstudoService(
            repositorio,
            new FakeUsuarioRepository(),
            new FakeQuadroNotificador());

        return (grupo, servico, repositorio);
    }

    private static TopicoEstudo AdicionarCard(GrupoEstudo grupo)
        => grupo.AdicionarTopico(
            DonoId,
            grupo.Colunas.OrderBy(coluna => coluna.Ordem).First().Id,
            "Card privado");

    [Fact]
    public async Task ObterDetalhesAsync_EstranhoNaoAcessaGrupo()
    {
        var (grupo, servico, _) = CriarCenario();

        await Assert.ThrowsAsync<DomainException>(
            () => servico.ObterDetalhesAsync(EstranhoId, grupo.Id));
    }

    [Fact]
    public async Task ObterDetalhesAsync_MembroAceitoAcessaGrupo()
    {
        var (grupo, servico, _) = CriarCenario();
        AdicionarCard(grupo);

        var detalhes = await servico.ObterDetalhesAsync(MembroId, grupo.Id);

        Assert.Equal(grupo.Id, detalhes.Id);
        Assert.Single(detalhes.Topicos);
    }

    [Fact]
    public async Task ExcluirGrupoAsync_MembroComumNaoExclui()
    {
        var (grupo, servico, repositorio) = CriarCenario();

        await Assert.ThrowsAsync<DomainException>(
            () => servico.ExcluirGrupoAsync(MembroId, grupo.Id));

        Assert.Equal(0, repositorio.Remocoes);
        Assert.NotNull(await repositorio.ObterPorIdAsync(grupo.Id));
    }

    [Fact]
    public async Task ConvidarMembroAsync_MembroComumNaoConvida()
    {
        var (grupo, servico, _) = CriarCenario();

        await Assert.ThrowsAsync<DomainException>(
            () => servico.ConvidarMembroAsync(
                MembroId,
                grupo.Id,
                new ConvidarMembroRequest("estranho@exemplo.com")));

        Assert.DoesNotContain(grupo.Membros, membro => membro.UsuarioId == EstranhoId);
    }

    [Fact]
    public async Task RemoverMembroAsync_MembroComumNaoRemoveOutroMembro()
    {
        var (grupo, servico, _) = CriarCenario();

        await Assert.ThrowsAsync<DomainException>(
            () => servico.RemoverMembroAsync(MembroId, grupo.Id, PendenteId));

        Assert.Contains(grupo.Membros, membro => membro.UsuarioId == PendenteId);
    }

    [Fact]
    public async Task ResponderConviteAsync_UsuarioNaoConvidadoNaoAceita()
    {
        var (grupo, servico, _) = CriarCenario();

        await Assert.ThrowsAsync<DomainException>(
            () => servico.ResponderConviteAsync(
                EstranhoId,
                grupo.Id,
                new ResponderConviteRequest(true)));

        Assert.DoesNotContain(grupo.Membros, membro => membro.UsuarioId == EstranhoId);
    }

    [Fact]
    public async Task AdicionarTopicoAsync_EstranhoNaoAdicionaCard()
    {
        var (grupo, servico, _) = CriarCenario();
        var coluna = grupo.Colunas.OrderBy(coluna => coluna.Ordem).First();

        await Assert.ThrowsAsync<DomainException>(
            () => servico.AdicionarTopicoAsync(
                EstranhoId,
                grupo.Id,
                new CriarTopicoRequest("Invasão", coluna.Id)));

        Assert.Empty(grupo.Topicos);
    }

    [Fact]
    public async Task MoverTopicoAsync_EstranhoNaoMoveCardDeOutroUsuario()
    {
        var (grupo, servico, _) = CriarCenario();
        var topico = AdicionarCard(grupo);
        var outraColuna = grupo.Colunas.OrderBy(coluna => coluna.Ordem).Skip(1).First();

        await Assert.ThrowsAsync<DomainException>(
            () => servico.MoverTopicoAsync(
                EstranhoId,
                topico.Id,
                new MoverTopicoRequest(outraColuna.Id, 0)));

        Assert.NotEqual(outraColuna.Id, topico.ColunaId);
    }
}
