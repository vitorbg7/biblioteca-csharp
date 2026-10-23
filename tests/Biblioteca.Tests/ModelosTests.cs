using Biblioteca.Core.Excecoes;
using Biblioteca.Core.Modelos;
using Xunit;

namespace Biblioteca.Tests;

public class LivroTests
{
    [Fact]
    public void CriaLivroValidoENormalizaOIsbn()
    {
        var livro = Livro.Novo("  Clean Code ", "Robert C. Martin", "978-0-13-235088-4", 2008, 2);

        Assert.Equal("Clean Code", livro.Titulo);
        Assert.Equal("9780132350884", livro.Isbn);
        Assert.Equal(0, livro.Id);
        Assert.Equal(7, livro.ComId(7).Id);
    }

    [Theory]
    [InlineData("", "Autor", "9780132350884", 2008, 1)]                // sem título
    [InlineData("Título", "  ", "9780132350884", 2008, 1)]             // sem autor
    [InlineData("Título", "Autor", "9780132350885", 2008, 1)]          // ISBN inválido
    [InlineData("Título", "Autor", "9780132350884", 1200, 1)]          // ano antigo demais
    [InlineData("Título", "Autor", "9780132350884", 2008, 0)]          // sem exemplares
    [InlineData("Título", "Autor", "9780132350884", 2008, 1001)]       // exemplares demais
    public void RejeitaDadosInvalidos(string titulo, string autor, string isbn, int ano, int quantidade)
    {
        Assert.Throws<RegraDeNegocioException>(() => Livro.Novo(titulo, autor, isbn, ano, quantidade));
    }
}

public class MembroTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 23);

    [Fact]
    public void CriaMembroENormalizaOEmail()
    {
        var membro = Membro.Novo("  Ana Souza ", " Ana.Souza@Exemplo.COM ", Hoje);

        Assert.Equal("Ana Souza", membro.Nome);
        Assert.Equal("ana.souza@exemplo.com", membro.Email);
    }

    [Theory]
    [InlineData("ana")]
    [InlineData("ana@")]
    [InlineData("ana@exemplo")]          // sem ponto no domínio
    [InlineData("a na@exemplo.com")]     // com espaço
    [InlineData("Ana <ana@exemplo.com>")] // formato com nome
    [InlineData("")]
    public void RejeitaEmailInvalido(string email)
    {
        Assert.Throws<RegraDeNegocioException>(() => Membro.Novo("Ana", email, Hoje));
    }

    [Fact]
    public void RejeitaNomeVazio()
    {
        Assert.Throws<RegraDeNegocioException>(() => Membro.Novo("   ", "ana@exemplo.com", Hoje));
    }
}
