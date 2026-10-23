using Biblioteca.Core.Excecoes;

namespace Biblioteca.Core.Modelos;

/// <summary>
/// Um título do acervo. Pode ter vários exemplares (QuantidadeTotal).
/// Quantos estão emprestados não fica aqui: isso é calculado a partir dos empréstimos em andamento.
/// </summary>
public sealed class Livro
{
    public const int TituloMaximo = 150;
    public const int AutorMaximo = 100;
    public const int ExemplaresMaximo = 1000;

    public Livro(int id, string titulo, string autor, string isbn, int anoPublicacao, int quantidadeTotal)
    {
        titulo = (titulo ?? string.Empty).Trim();
        autor = (autor ?? string.Empty).Trim();
        isbn = CodigoIsbn.Normalizar(isbn);

        if (titulo.Length == 0) throw new RegraDeNegocioException("Informe o título do livro.");
        if (titulo.Length > TituloMaximo) throw new RegraDeNegocioException($"O título pode ter até {TituloMaximo} caracteres.");
        if (autor.Length == 0) throw new RegraDeNegocioException("Informe o autor do livro.");
        if (autor.Length > AutorMaximo) throw new RegraDeNegocioException($"O nome do autor pode ter até {AutorMaximo} caracteres.");
        if (!CodigoIsbn.EhValido(isbn)) throw new RegraDeNegocioException("ISBN inválido. Informe os 13 dígitos, com ou sem hífens.");
        if (anoPublicacao < 1400 || anoPublicacao > 2100) throw new RegraDeNegocioException("Informe um ano de publicação entre 1400 e 2100.");
        if (quantidadeTotal < 1 || quantidadeTotal > ExemplaresMaximo) throw new RegraDeNegocioException($"A quantidade de exemplares deve ficar entre 1 e {ExemplaresMaximo}.");

        Id = id;
        Titulo = titulo;
        Autor = autor;
        Isbn = isbn;
        AnoPublicacao = anoPublicacao;
        QuantidadeTotal = quantidadeTotal;
    }

    /// <summary>Id 0 significa "ainda não salvo": o banco é quem gera o número.</summary>
    public int Id { get; }
    public string Titulo { get; }
    public string Autor { get; }
    public string Isbn { get; }
    public int AnoPublicacao { get; }
    public int QuantidadeTotal { get; }

    public static Livro Novo(string titulo, string autor, string isbn, int anoPublicacao, int quantidadeTotal) =>
        new(0, titulo, autor, isbn, anoPublicacao, quantidadeTotal);

    /// <summary>Cópia com o Id que o banco gerou.</summary>
    public Livro ComId(int id) => new(id, Titulo, Autor, Isbn, AnoPublicacao, QuantidadeTotal);
}
