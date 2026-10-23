using Biblioteca.Core.Excecoes;
using Biblioteca.Core.Modelos;
using Xunit;

namespace Biblioteca.Tests;

public class EmprestimoTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 23);

    [Fact]
    public void NovoEmprestimoTemPrazoDe14Dias()
    {
        var emprestimo = Emprestimo.Novo(1, 1, Hoje);

        Assert.Equal(Hoje.AddDays(14), emprestimo.DataPrevistaDevolucao);
        Assert.True(emprestimo.EmAndamento);
        Assert.Equal(0m, emprestimo.Multa);
    }

    [Fact]
    public void NoUltimoDiaDoPrazoAindaNaoEstaAtrasado()
    {
        var emprestimo = Emprestimo.Novo(1, 1, Hoje);

        Assert.False(emprestimo.EstaAtrasado(Hoje.AddDays(14)));
        Assert.True(emprestimo.EstaAtrasado(Hoje.AddDays(15)));
    }

    [Fact]
    public void DevolverNoPrazoNaoGeraMulta()
    {
        var devolvido = Emprestimo.Novo(1, 1, Hoje).Devolver(Hoje.AddDays(14));

        Assert.False(devolvido.EmAndamento);
        Assert.Equal(0m, devolvido.Multa);
    }

    [Fact]
    public void DevolverComAtrasoCobraMultaPorDia()
    {
        var devolvido = Emprestimo.Novo(1, 1, Hoje).Devolver(Hoje.AddDays(17)); // 3 dias depois do prazo

        Assert.Equal(3, devolvido.DiasDeAtraso(Hoje.AddDays(17)));
        Assert.Equal(3.00m, devolvido.Multa);
    }

    [Fact]
    public void NaoPermiteDevolverDuasVezes()
    {
        var devolvido = Emprestimo.Novo(1, 1, Hoje).Devolver(Hoje.AddDays(1));

        Assert.Throws<RegraDeNegocioException>(() => devolvido.Devolver(Hoje.AddDays(2)));
    }

    [Fact]
    public void NaoPermiteDevolverAntesDoEmprestimo()
    {
        var emprestimo = Emprestimo.Novo(1, 1, Hoje);

        Assert.Throws<RegraDeNegocioException>(() => emprestimo.Devolver(Hoje.AddDays(-1)));
    }
}
