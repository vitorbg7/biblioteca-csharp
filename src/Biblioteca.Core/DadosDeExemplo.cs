using Biblioteca.Core.Modelos;
using Biblioteca.Core.Repositorios;

namespace Biblioteca.Core;

/// <summary>
/// Dados de exemplo para o modo "--memoria". Inclui empréstimos antigos, um em atraso e
/// um já devolvido com multa, para que todas as telas tenham o que mostrar.
/// (No SQL Server, o equivalente é o script database/02_dados_exemplo.sql.)
/// </summary>
public static class DadosDeExemplo
{
    public static void Carregar(ILivroRepositorio livros, IMembroRepositorio membros,
        IEmprestimoRepositorio emprestimos, DateOnly hoje)
    {
        var cleanCode = livros.Adicionar(Livro.Novo("Clean Code", "Robert C. Martin", "9780132350884", 2008, 2));
        var designPatterns = livros.Adicionar(Livro.Novo("Design Patterns", "Erich Gamma", "9780201633610", 1994, 1));
        var effectiveJava = livros.Adicionar(Livro.Novo("Effective Java", "Joshua Bloch", "9780134685991", 2018, 3));
        var pragmatico = livros.Adicionar(Livro.Novo("The Pragmatic Programmer", "David Thomas", "9780135957059", 2019, 2));
        livros.Adicionar(Livro.Novo("Refactoring", "Martin Fowler", "9780134757599", 2018, 1));
        livros.Adicionar(Livro.Novo("Introduction to Algorithms", "Thomas H. Cormen", "9780262033848", 2009, 2));

        var ana = membros.Adicionar(Membro.Novo("Ana Souza", "ana.souza@exemplo.com", hoje.AddDays(-90)));
        var bruno = membros.Adicionar(Membro.Novo("Bruno Lima", "bruno.lima@exemplo.com", hoje.AddDays(-60)));
        var carla = membros.Adicionar(Membro.Novo("Carla Dias", "carla.dias@exemplo.com", hoje.AddDays(-30)));
        membros.Adicionar(Membro.Novo("Diego Rocha", "diego.rocha@exemplo.com", hoje.AddDays(-5)));

        // Histórico já devolvido (primeiro, para não ocupar exemplares).
        Registrar(emprestimos, designPatterns, carla, hoje.AddDays(-40), devolvidoEm: hoje.AddDays(-30));
        Registrar(emprestimos, pragmatico, carla, hoje.AddDays(-40), devolvidoEm: hoje.AddDays(-18)); // 8 dias de atraso

        // Em andamento.
        Registrar(emprestimos, cleanCode, ana, hoje.AddDays(-20));         // atrasado (prazo venceu há 6 dias)
        Registrar(emprestimos, effectiveJava, bruno, hoje.AddDays(-5));
        Registrar(emprestimos, cleanCode, bruno, hoje.AddDays(-3));        // os 2 exemplares de Clean Code ficam fora
    }

    private static void Registrar(IEmprestimoRepositorio emprestimos, int livroId, int membroId,
        DateOnly emprestadoEm, DateOnly? devolvidoEm = null)
    {
        var previsto = emprestadoEm.AddDays(Emprestimo.PrazoEmDias);
        var multa = 0m;
        if (devolvidoEm is not null)
        {
            multa = new Emprestimo(0, livroId, membroId, emprestadoEm, previsto, devolvidoEm, 0m)
                .CalcularMulta(devolvidoEm.Value);
        }

        emprestimos.TentarRegistrar(new Emprestimo(0, livroId, membroId, emprestadoEm, previsto, devolvidoEm, multa));
    }
}
