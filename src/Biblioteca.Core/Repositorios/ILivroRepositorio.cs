using Biblioteca.Core.Modelos;

namespace Biblioteca.Core.Repositorios;

public interface ILivroRepositorio
{
    /// <summary>Salva o livro e devolve o Id gerado.</summary>
    int Adicionar(Livro livro);

    Livro? ObterPorId(int id);

    bool IsbnExiste(string isbn);

    /// <summary>Lista os livros com a disponibilidade. Com termo, filtra por título ou autor (sem diferenciar maiúsculas e acentos).</summary>
    IReadOnlyList<LivroComDisponibilidade> Listar(string? termo = null);

    IReadOnlyList<LivroMaisEmprestado> MaisEmprestados(int limite);
}
