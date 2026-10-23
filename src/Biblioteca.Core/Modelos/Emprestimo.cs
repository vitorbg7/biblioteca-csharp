using Biblioteca.Core.Excecoes;

namespace Biblioteca.Core.Modelos;

/// <summary>
/// Registro de um livro emprestado a um membro. Enquanto DataDevolucao for nula, o empréstimo está em andamento.
/// Prazo e multa seguem as constantes abaixo.
/// </summary>
public sealed class Emprestimo
{
    public const int PrazoEmDias = 14;
    public const decimal MultaPorDiaDeAtraso = 1.00m;

    public Emprestimo(int id, int livroId, int membroId, DateOnly dataEmprestimo,
        DateOnly dataPrevistaDevolucao, DateOnly? dataDevolucao, decimal multa)
    {
        if (dataPrevistaDevolucao < dataEmprestimo)
            throw new RegraDeNegocioException("A data prevista de devolução não pode ser anterior ao empréstimo.");
        if (dataDevolucao is not null && dataDevolucao < dataEmprestimo)
            throw new RegraDeNegocioException("A devolução não pode ser anterior ao empréstimo.");
        if (multa < 0)
            throw new RegraDeNegocioException("A multa não pode ser negativa.");

        Id = id;
        LivroId = livroId;
        MembroId = membroId;
        DataEmprestimo = dataEmprestimo;
        DataPrevistaDevolucao = dataPrevistaDevolucao;
        DataDevolucao = dataDevolucao;
        Multa = multa;
    }

    public int Id { get; }
    public int LivroId { get; }
    public int MembroId { get; }
    public DateOnly DataEmprestimo { get; }
    public DateOnly DataPrevistaDevolucao { get; }
    public DateOnly? DataDevolucao { get; }
    public decimal Multa { get; }

    public bool EmAndamento => DataDevolucao is null;

    public static Emprestimo Novo(int livroId, int membroId, DateOnly hoje) =>
        new(0, livroId, membroId, hoje, hoje.AddDays(PrazoEmDias), null, 0m);

    public Emprestimo ComId(int id) =>
        new(id, LivroId, MembroId, DataEmprestimo, DataPrevistaDevolucao, DataDevolucao, Multa);

    /// <summary>Está atrasado se ainda não foi devolvido e o prazo já passou.</summary>
    public bool EstaAtrasado(DateOnly hoje) => EmAndamento && hoje > DataPrevistaDevolucao;

    /// <summary>
    /// Dias entre o prazo e a devolução (ou a data de referência, se ainda não foi devolvido).
    /// Devolver no último dia do prazo não conta como atraso.
    /// </summary>
    public int DiasDeAtraso(DateOnly referencia)
    {
        var fim = DataDevolucao ?? referencia;
        return Math.Max(0, fim.DayNumber - DataPrevistaDevolucao.DayNumber);
    }

    public decimal CalcularMulta(DateOnly referencia) => DiasDeAtraso(referencia) * MultaPorDiaDeAtraso;

    /// <summary>Devolve uma cópia já marcada como devolvida, com a multa calculada.</summary>
    public Emprestimo Devolver(DateOnly hoje)
    {
        if (!EmAndamento)
            throw new RegraDeNegocioException("Esse empréstimo já foi devolvido.");
        if (hoje < DataEmprestimo)
            throw new RegraDeNegocioException("A devolução não pode ser anterior ao empréstimo.");

        return new Emprestimo(Id, LivroId, MembroId, DataEmprestimo, DataPrevistaDevolucao, hoje, CalcularMulta(hoje));
    }
}
