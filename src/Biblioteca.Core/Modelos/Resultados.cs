namespace Biblioteca.Core.Modelos;

/// <summary>Livro com a contagem de exemplares que estão emprestados agora.</summary>
public sealed record LivroComDisponibilidade(Livro Livro, int Emprestados)
{
    public int Disponiveis => Livro.QuantidadeTotal - Emprestados;
}

/// <summary>Linha do ranking de livros mais emprestados.</summary>
public sealed record LivroMaisEmprestado(string Titulo, string Autor, int TotalEmprestimos);

/// <summary>Empréstimo junto com o título e o nome do membro, para exibir em listas.</summary>
public sealed record EmprestimoDetalhado(Emprestimo Emprestimo, string TituloLivro, string NomeMembro);
