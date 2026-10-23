using System.Data;
using Biblioteca.Core.Modelos;
using Biblioteca.Core.Repositorios;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Data;

public sealed class LivroRepositorioSql : ILivroRepositorio
{
    private readonly string _conexao;

    public LivroRepositorioSql(string textoDeConexao) => _conexao = textoDeConexao;

    public int Adicionar(Livro livro)
    {
        using var conexao = ConexaoSql.Abrir(_conexao);
        using var comando = conexao.CreateCommand();
        comando.CommandText = @"
            INSERT INTO dbo.Livros (Titulo, Autor, Isbn, AnoPublicacao, QuantidadeTotal)
            OUTPUT INSERTED.Id
            VALUES (@Titulo, @Autor, @Isbn, @Ano, @Quantidade)";
        comando.Parametro("@Titulo", SqlDbType.NVarChar, livro.Titulo);
        comando.Parametro("@Autor", SqlDbType.NVarChar, livro.Autor);
        comando.Parametro("@Isbn", SqlDbType.Char, livro.Isbn);
        comando.Parametro("@Ano", SqlDbType.SmallInt, (short)livro.AnoPublicacao);
        comando.Parametro("@Quantidade", SqlDbType.Int, livro.QuantidadeTotal);
        return (int)comando.ExecuteScalar()!;
    }

    public Livro? ObterPorId(int id)
    {
        using var conexao = ConexaoSql.Abrir(_conexao);
        using var comando = conexao.CreateCommand();
        comando.CommandText = @"
            SELECT Id, Titulo, Autor, Isbn, AnoPublicacao, QuantidadeTotal
            FROM dbo.Livros
            WHERE Id = @Id";
        comando.Parametro("@Id", SqlDbType.Int, id);

        using var leitor = comando.ExecuteReader();
        return leitor.Read() ? LerLivro(leitor) : null;
    }

    public bool IsbnExiste(string isbn)
    {
        using var conexao = ConexaoSql.Abrir(_conexao);
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT COUNT(1) FROM dbo.Livros WHERE Isbn = @Isbn";
        comando.Parametro("@Isbn", SqlDbType.Char, isbn);
        return (int)comando.ExecuteScalar()! > 0;
    }

    public IReadOnlyList<LivroComDisponibilidade> Listar(string? termo = null)
    {
        using var conexao = ConexaoSql.Abrir(_conexao);
        using var comando = conexao.CreateCommand();

        // A view vw_LivrosComDisponibilidade já calcula quantos exemplares estão emprestados.
        // O COLLATE ..._CI_AI faz a busca ignorar maiúsculas (CI) e acentos (AI).
        comando.CommandText = @"
            SELECT Id, Titulo, Autor, Isbn, AnoPublicacao, QuantidadeTotal, Emprestados
            FROM dbo.vw_LivrosComDisponibilidade
            WHERE @Padrao IS NULL
               OR Titulo COLLATE Latin1_General_CI_AI LIKE @Padrao
               OR Autor COLLATE Latin1_General_CI_AI LIKE @Padrao
            ORDER BY Titulo";
        comando.Parametro("@Padrao", SqlDbType.NVarChar,
            string.IsNullOrWhiteSpace(termo) ? null : "%" + EscaparLike(termo.Trim()) + "%");

        var livros = new List<LivroComDisponibilidade>();
        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
        {
            livros.Add(new LivroComDisponibilidade(LerLivro(leitor), leitor.GetInt32(6)));
        }

        return livros;
    }

    public IReadOnlyList<LivroMaisEmprestado> MaisEmprestados(int limite)
    {
        using var conexao = ConexaoSql.Abrir(_conexao);
        using var comando = conexao.CreateCommand();
        comando.CommandText = @"
            SELECT TOP (@Limite) l.Titulo, l.Autor, COUNT(e.Id) AS Total
            FROM dbo.Livros AS l
            INNER JOIN dbo.Emprestimos AS e ON e.LivroId = l.Id
            GROUP BY l.Id, l.Titulo, l.Autor
            ORDER BY Total DESC, l.Titulo";
        comando.Parametro("@Limite", SqlDbType.Int, limite);

        var ranking = new List<LivroMaisEmprestado>();
        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
        {
            ranking.Add(new LivroMaisEmprestado(leitor.GetString(0), leitor.GetString(1), leitor.GetInt32(2)));
        }

        return ranking;
    }

    private static Livro LerLivro(SqlDataReader leitor) =>
        new(leitor.GetInt32(0), leitor.GetString(1), leitor.GetString(2), leitor.GetString(3),
            leitor.GetInt16(4), leitor.GetInt32(5));

    /// <summary>No LIKE, os caracteres % _ [ têm significado especial; aqui viram texto comum.</summary>
    private static string EscaparLike(string texto) =>
        texto.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
