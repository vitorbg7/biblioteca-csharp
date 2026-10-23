using Biblioteca.Core.Excecoes;
using Biblioteca.Core.Repositorios;
using Biblioteca.Core.Servicos;
using Xunit;

namespace Biblioteca.Tests;

public class BibliotecaServiceTests
{
    // ISBNs válidos (dígito verificador conferido).
    private const string IsbnA = "9780132350884";
    private const string IsbnB = "9780201633610";
    private const string IsbnC = "9780134685991";
    private const string IsbnD = "9780135957059";

    private readonly RelogioFalso _relogio = new(new DateOnly(2026, 10, 23));
    private readonly BibliotecaService _servico;

    public BibliotecaServiceTests()
    {
        var banco = new BancoEmMemoria();
        _servico = new BibliotecaService(banco, banco, banco, _relogio);
    }

    // ---------- Cadastros ----------

    [Fact]
    public void CadastraLivroEDevolveComId()
    {
        var livro = _servico.CadastrarLivro("Clean Code", "Robert C. Martin", IsbnA, 2008, 2);

        Assert.Equal(1, livro.Id);
        Assert.Single(_servico.ListarLivros());
    }

    [Fact]
    public void NaoPermiteIsbnDuplicado()
    {
        _servico.CadastrarLivro("Clean Code", "Robert C. Martin", IsbnA, 2008, 2);

        // Mesmo ISBN escrito com hífens: a comparação é feita depois de normalizar.
        Assert.Throws<RegraDeNegocioException>(() =>
            _servico.CadastrarLivro("Outra edição", "Outro autor", "978-0-13-235088-4", 2010, 1));
    }

    [Fact]
    public void NaoPermiteEmailDuplicadoMesmoComMaiusculas()
    {
        _servico.CadastrarMembro("Ana", "ana@exemplo.com");

        Assert.Throws<RegraDeNegocioException>(() => _servico.CadastrarMembro("Outra Ana", "ANA@Exemplo.com"));
    }

    // ---------- Empréstimos ----------

    [Fact]
    public void EmprestaLivroComPrazoDe14DiasEReduzDisponibilidade()
    {
        var livro = _servico.CadastrarLivro("Clean Code", "Robert C. Martin", IsbnA, 2008, 2);
        var membro = _servico.CadastrarMembro("Ana", "ana@exemplo.com");

        var emprestimo = _servico.Emprestar(livro.Id, membro.Id);

        Assert.Equal(new DateOnly(2026, 11, 6), emprestimo.DataPrevistaDevolucao);
        Assert.Equal(1, _servico.ListarLivros()[0].Disponiveis);
    }

    [Fact]
    public void LivroOuMembroInexistenteLancaNaoEncontrado()
    {
        var livro = _servico.CadastrarLivro("Clean Code", "Robert C. Martin", IsbnA, 2008, 1);
        var membro = _servico.CadastrarMembro("Ana", "ana@exemplo.com");

        Assert.Throws<NaoEncontradoException>(() => _servico.Emprestar(999, membro.Id));
        Assert.Throws<NaoEncontradoException>(() => _servico.Emprestar(livro.Id, 999));
    }

    [Fact]
    public void NaoEmprestaQuandoNaoHaExemplarLivre()
    {
        var livro = _servico.CadastrarLivro("Design Patterns", "Erich Gamma", IsbnB, 1994, 1);
        var ana = _servico.CadastrarMembro("Ana", "ana@exemplo.com");
        var bruno = _servico.CadastrarMembro("Bruno", "bruno@exemplo.com");
        _servico.Emprestar(livro.Id, ana.Id);

        var erro = Assert.Throws<RegraDeNegocioException>(() => _servico.Emprestar(livro.Id, bruno.Id));
        Assert.Contains("exemplar", erro.Message);
    }

    [Fact]
    public void LimitaTresEmprestimosPorMembro()
    {
        var membro = _servico.CadastrarMembro("Ana", "ana@exemplo.com");
        var l1 = _servico.CadastrarLivro("Livro 1", "Autor", IsbnA, 2000, 5);
        var l2 = _servico.CadastrarLivro("Livro 2", "Autor", IsbnB, 2000, 5);
        var l3 = _servico.CadastrarLivro("Livro 3", "Autor", IsbnC, 2000, 5);
        var l4 = _servico.CadastrarLivro("Livro 4", "Autor", IsbnD, 2000, 5);
        _servico.Emprestar(l1.Id, membro.Id);
        _servico.Emprestar(l2.Id, membro.Id);
        _servico.Emprestar(l3.Id, membro.Id);

        var erro = Assert.Throws<RegraDeNegocioException>(() => _servico.Emprestar(l4.Id, membro.Id));
        Assert.Contains("limite", erro.Message);
    }

    [Fact]
    public void MembroComLivroAtrasadoNaoPodePegarOutro()
    {
        var membro = _servico.CadastrarMembro("Ana", "ana@exemplo.com");
        var l1 = _servico.CadastrarLivro("Livro 1", "Autor", IsbnA, 2000, 2);
        var l2 = _servico.CadastrarLivro("Livro 2", "Autor", IsbnB, 2000, 2);
        _servico.Emprestar(l1.Id, membro.Id);

        _relogio.AvancarDias(15); // prazo era 14 dias: está 1 dia atrasado

        var erro = Assert.Throws<RegraDeNegocioException>(() => _servico.Emprestar(l2.Id, membro.Id));
        Assert.Contains("atraso", erro.Message);
    }

    // ---------- Devolução ----------

    [Fact]
    public void DevolverComAtrasoCobraMultaELiberaOExemplar()
    {
        var livro = _servico.CadastrarLivro("Clean Code", "Robert C. Martin", IsbnA, 2008, 1);
        var membro = _servico.CadastrarMembro("Ana", "ana@exemplo.com");
        var emprestimo = _servico.Emprestar(livro.Id, membro.Id);

        _relogio.AvancarDias(17); // 3 dias depois do prazo
        var devolvido = _servico.Devolver(emprestimo.Id);

        Assert.Equal(3.00m, devolvido.Multa);
        Assert.Equal(1, _servico.ListarLivros()[0].Disponiveis);
        Assert.Empty(_servico.EmprestimosEmAndamento());
    }

    [Fact]
    public void DevolverEmprestimoInexistenteOuJaDevolvidoFalha()
    {
        var livro = _servico.CadastrarLivro("Clean Code", "Robert C. Martin", IsbnA, 2008, 1);
        var membro = _servico.CadastrarMembro("Ana", "ana@exemplo.com");
        var emprestimo = _servico.Emprestar(livro.Id, membro.Id);
        _servico.Devolver(emprestimo.Id);

        Assert.Throws<NaoEncontradoException>(() => _servico.Devolver(999));
        Assert.Throws<RegraDeNegocioException>(() => _servico.Devolver(emprestimo.Id));
    }

    // ---------- Consultas ----------

    [Fact]
    public void ListaAtrasadosSomenteOsVencidos()
    {
        var ana = _servico.CadastrarMembro("Ana", "ana@exemplo.com");
        var bruno = _servico.CadastrarMembro("Bruno", "bruno@exemplo.com");
        var l1 = _servico.CadastrarLivro("Livro 1", "Autor", IsbnA, 2000, 2);
        var l2 = _servico.CadastrarLivro("Livro 2", "Autor", IsbnB, 2000, 2);
        _servico.Emprestar(l1.Id, ana.Id);

        _relogio.AvancarDias(10);
        _servico.Emprestar(l2.Id, bruno.Id);
        _relogio.AvancarDias(5); // empréstimo da Ana: 15 dias (atrasado); do Bruno: 5 dias (no prazo)

        var atrasados = _servico.EmprestimosAtrasados();

        Assert.Single(atrasados);
        Assert.Equal("Ana", atrasados[0].NomeMembro);
        Assert.Equal(2, _servico.EmprestimosEmAndamento().Count);
    }

    [Fact]
    public void RankingOrdenaDoMaisParaOMenosEmprestado()
    {
        var ana = _servico.CadastrarMembro("Ana", "ana@exemplo.com");
        var l1 = _servico.CadastrarLivro("Livro A", "Autor", IsbnA, 2000, 5);
        var l2 = _servico.CadastrarLivro("Livro B", "Autor", IsbnB, 2000, 5);
        _servico.Devolver(_servico.Emprestar(l1.Id, ana.Id).Id);
        _servico.Devolver(_servico.Emprestar(l2.Id, ana.Id).Id);
        _servico.Devolver(_servico.Emprestar(l2.Id, ana.Id).Id);

        var ranking = _servico.LivrosMaisEmprestados(5);

        Assert.Equal("Livro B", ranking[0].Titulo);
        Assert.Equal(2, ranking[0].TotalEmprestimos);
        Assert.Equal("Livro A", ranking[1].Titulo);
    }

    [Fact]
    public void BuscaIgnoraMaiusculasEAcentos()
    {
        _servico.CadastrarLivro("Código Limpo", "Robert C. Martin", IsbnA, 2009, 1);
        _servico.CadastrarLivro("Design Patterns", "Erich Gamma", IsbnB, 1994, 1);

        Assert.Single(_servico.ListarLivros("CODIGO"));
        Assert.Single(_servico.ListarLivros("gamma"));
        Assert.Empty(_servico.ListarLivros("inexistente"));
        Assert.Equal(2, _servico.ListarLivros().Count);
    }
}
