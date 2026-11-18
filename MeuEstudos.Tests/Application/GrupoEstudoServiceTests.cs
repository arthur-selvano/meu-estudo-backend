using MeuEstudos.Application.DTOs;
using MeuEstudos.Application.Services;
using MeuEstudos.Domain.Entities;
using MeuEstudos.Domain.Enums;
using MeuEstudos.Domain.Exceptions;
using MeuEstudos.Tests.Fakes;

namespace MeuEstudos.Tests.Application;

public sealed class GrupoEstudoServiceTests
{
    [Fact]
    public async Task AdicionarTopicoAsync_EncaminhaOGuidDaColunaDaRequisicao()
    {
        var donoId = Guid.Parse("d0642713-a0e3-4fbf-a486-ded7452fa975");
        var grupo = new GrupoEstudo("Grupo", null, TipoGrupo.TrabalhoAcademico, donoId);
        var colunaAFazer = grupo.Colunas.OrderBy(coluna => coluna.Ordem).First();
        var repositorio = new FakeGrupoEstudoRepository(grupo);
        var servico = new GrupoEstudoService(repositorio, new FakeUsuarioRepository(), new FakeQuadroNotificador());

        var resposta = await servico.AdicionarTopicoAsync(
            donoId,
            grupo.Id,
            new CriarTopicoRequest("COmer", colunaAFazer.Id));

        Assert.Equal(colunaAFazer.Id, resposta.ColunaId);
        Assert.Equal("A Fazer", grupo.Colunas.Single(coluna => coluna.Id == resposta.ColunaId).Titulo);
        Assert.Equal(resposta.Id, Assert.Single(grupo.Topicos).Id);
        Assert.Equal(1, repositorio.Atualizacoes);
    }

    [Fact]
    public async Task AlterarResponsavelTopicoAsync_AtribuiMembroERetornaDadosParaExibicao()
    {
        var donoId = Guid.Parse("d0642713-a0e3-4fbf-a486-ded7452fa975");
        var membroId = Guid.Parse("0b5f8f03-2a84-4c8d-9f5f-5c3e1f2a7b11");
        var grupo = new GrupoEstudo("Grupo", null, TipoGrupo.TrabalhoAcademico, donoId);
        grupo.ConvidarMembro(donoId, membroId);
        grupo.ResponderConvite(membroId, true);
        var topico = grupo.AdicionarTopico(
            donoId,
            grupo.Colunas.OrderBy(coluna => coluna.Ordem).First().Id,
            "Card");

        var repositorio = new FakeGrupoEstudoRepository(grupo);
        var usuarios = new FakeUsuarioRepository(
            new Dictionary<Guid, UsuarioResumo>
            {
                [membroId] = new(membroId, "Membro Aceito", "/uploads/perfis/membro.png")
            });
        var servico = new GrupoEstudoService(repositorio, usuarios, new FakeQuadroNotificador());

        var resposta = await servico.AlterarResponsavelTopicoAsync(
            donoId,
            topico.Id,
            new AlterarResponsavelTopicoRequest(membroId));

        Assert.Equal(membroId, resposta.ResponsavelId);
        Assert.Equal("Membro Aceito", resposta.ResponsavelNome);
        Assert.Equal("/uploads/perfis/membro.png", resposta.ResponsavelFotoUrl);
        Assert.Equal(membroId, topico.ResponsavelId);
        Assert.Equal(1, repositorio.Atualizacoes);
    }

    [Fact]
    public async Task AlterarResponsavelTopicoAsync_RemoveResponsavelComNull()
    {
        var donoId = Guid.Parse("d0642713-a0e3-4fbf-a486-ded7452fa975");
        var grupo = new GrupoEstudo("Grupo", null, TipoGrupo.TrabalhoAcademico, donoId);
        var topico = grupo.AdicionarTopico(
            donoId,
            grupo.Colunas.OrderBy(coluna => coluna.Ordem).First().Id,
            "Card",
            responsavelId: donoId);
        var repositorio = new FakeGrupoEstudoRepository(grupo);
        var servico = new GrupoEstudoService(repositorio, new FakeUsuarioRepository(), new FakeQuadroNotificador());

        var resposta = await servico.AlterarResponsavelTopicoAsync(
            donoId,
            topico.Id,
            new AlterarResponsavelTopicoRequest(null));

        Assert.Null(resposta.ResponsavelId);
        Assert.Null(resposta.ResponsavelNome);
        Assert.Null(topico.ResponsavelId);
        Assert.Equal(1, repositorio.Atualizacoes);
    }

    [Fact]
    public async Task AdicionarTopicoAsync_RejeitaColunaVaziaSemPersistir()
    {
        var donoId = Guid.Parse("d0642713-a0e3-4fbf-a486-ded7452fa975");
        var grupo = new GrupoEstudo("Grupo", null, TipoGrupo.TrabalhoAcademico, donoId);
        var repositorio = new FakeGrupoEstudoRepository(grupo);
        var servico = new GrupoEstudoService(repositorio, new FakeUsuarioRepository(), new FakeQuadroNotificador());

        var excecao = await Assert.ThrowsAsync<DomainException>(
            () => servico.AdicionarTopicoAsync(
                donoId,
                grupo.Id,
                new CriarTopicoRequest("COmer", Guid.Empty)));

        Assert.Equal("A coluna do card é obrigatória.", excecao.Message);
        Assert.Empty(grupo.Topicos);
        Assert.Equal(0, repositorio.Atualizacoes);
    }

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
}
