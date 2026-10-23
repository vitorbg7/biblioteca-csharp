using System.Data;
using Biblioteca.Core.Excecoes;
using Biblioteca.Core.Modelos;
using Biblioteca.Core.Repositorios;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Data;

public sealed class EmprestimoRepositorioSql : IEmprestimoRepositorio
{
    // Colunas na ordem em que LerEmprestimo espera.
    private const string ColunasEmprestimo =
        "e.Id, e.LivroId, e.MembroId, e.DataEmprestimo, e.DataPrevistaDevolucao, e.DataDevolucao, e.Multa";

    private readonly string _conexao;

    public EmprestimoRepositorioSql(string textoDeConexao) => _conexao = textoDeConexao;

    public int ContarEmAndamentoDoMembro(int membroId)
    {
        using var conexao = ConexaoSql.Abrir(_conexao);
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT COUNT(1) FROM dbo.Emprestimos WHERE MembroId = @MembroId AND DataDevolucao IS NULL";
        comando.Parametro("@MembroId", SqlDbType.Int, membroId);
        return (int)comando.ExecuteScalar()!;
    }

    public bool MembroTemAtraso(int membroId, DateOnly hoje)
    {
        using var conexao = ConexaoSql.Abrir(_conexao);
        using var comando = conexao.CreateCommand();
        comando.CommandText = @"
            SELECT COUNT(1)
            FROM dbo.Emprestimos
            WHERE MembroId = @MembroId AND DataDevolucao IS NULL AND DataPrevistaDevolucao < @Hoje";
        comando.Parametro("@MembroId", SqlDbType.Int, membroId);
        comando.Parametro("@Hoje", SqlDbType.Date, hoje.ParaDateTime());
        return (int)comando.ExecuteScalar()! > 0;
    }

    public int? TentarRegistrar(Emprestimo emprestimo)
    {
        using var conexao = ConexaoSql.Abrir(_conexao);
        using var transacao = conexao.BeginTransaction();

        // Trava a linha do livro até o fim da transação (UPDLOCK + HOLDLOCK). Se duas pessoas tentarem
        // pegar o último exemplar ao mesmo tempo, a segunda espera a primeira terminar e já enxerga
        // o resultado, em vez de as duas passarem na conferência.
        int disponiveis;
        using (var consulta = conexao.CreateCommand())
        {
            consulta.Transaction = transacao;
            consulta.CommandText = @"
                SELECT l.QuantidadeTotal - (SELECT COUNT(1)
                                            FROM dbo.Emprestimos AS e
                                            WHERE e.LivroId = l.Id AND e.DataDevolucao IS NULL)
                FROM dbo.Livros AS l WITH (UPDLOCK, HOLDLOCK)
                WHERE l.Id = @LivroId";
            consulta.Parametro("@LivroId", SqlDbType.Int, emprestimo.LivroId);

            var resultado = consulta.ExecuteScalar();
            if (resultado is null)
            {
                return null; // livro não existe
            }

            disponiveis = (int)resultado;
        }

        // Empréstimo já devolvido (usado só ao importar histórico) não ocupa exemplar.
        if (emprestimo.EmAndamento && disponiveis <= 0)
        {
            return null; // sem exemplar livre; o rollback acontece ao descartar a transação
        }

        int id;
        using (var insercao = conexao.CreateCommand())
        {
            insercao.Transaction = transacao;
            insercao.CommandText = @"
                INSERT INTO dbo.Emprestimos (LivroId, MembroId, DataEmprestimo, DataPrevistaDevolucao, DataDevolucao, Multa)
                OUTPUT INSERTED.Id
                VALUES (@LivroId, @MembroId, @DataEmprestimo, @DataPrevista, @DataDevolucao, @Multa)";
            insercao.Parametro("@LivroId", SqlDbType.Int, emprestimo.LivroId);
            insercao.Parametro("@MembroId", SqlDbType.Int, emprestimo.MembroId);
            insercao.Parametro("@DataEmprestimo", SqlDbType.Date, emprestimo.DataEmprestimo.ParaDateTime());
            insercao.Parametro("@DataPrevista", SqlDbType.Date, emprestimo.DataPrevistaDevolucao.ParaDateTime());
            insercao.Parametro("@DataDevolucao", SqlDbType.Date, emprestimo.DataDevolucao?.ParaDateTime());
            insercao.ParametroMoeda("@Multa", emprestimo.Multa);
            id = (int)insercao.ExecuteScalar()!;
        }

        transacao.Commit();
        return id;
    }

    public Emprestimo? ObterPorId(int id)
    {
        using var conexao = ConexaoSql.Abrir(_conexao);
        using var comando = conexao.CreateCommand();
        comando.CommandText = $"SELECT {ColunasEmprestimo} FROM dbo.Emprestimos AS e WHERE e.Id = @Id";
        comando.Parametro("@Id", SqlDbType.Int, id);

        using var leitor = comando.ExecuteReader();
        return leitor.Read() ? LerEmprestimo(leitor) : null;
    }

    public void RegistrarDevolucao(Emprestimo devolvido)
    {
        using var conexao = ConexaoSql.Abrir(_conexao);
        using var comando = conexao.CreateCommand();

        // "AND DataDevolucao IS NULL" garante que só atualiza se ainda estiver em andamento.
        comando.CommandText = @"
            UPDATE dbo.Emprestimos
            SET DataDevolucao = @DataDevolucao, Multa = @Multa
            WHERE Id = @Id AND DataDevolucao IS NULL";
        comando.Parametro("@Id", SqlDbType.Int, devolvido.Id);
        comando.Parametro("@DataDevolucao", SqlDbType.Date, devolvido.DataDevolucao?.ParaDateTime());
        comando.ParametroMoeda("@Multa", devolvido.Multa);

        if (comando.ExecuteNonQuery() == 0)
        {
            throw new RegraDeNegocioException("Esse empréstimo já foi devolvido.");
        }
    }

    public IReadOnlyList<EmprestimoDetalhado> ListarEmAndamento() => ListarDetalhados(somenteAtrasados: false, hoje: null);

    public IReadOnlyList<EmprestimoDetalhado> ListarAtrasados(DateOnly hoje) => ListarDetalhados(somenteAtrasados: true, hoje);

    private IReadOnlyList<EmprestimoDetalhado> ListarDetalhados(bool somenteAtrasados, DateOnly? hoje)
    {
        using var conexao = ConexaoSql.Abrir(_conexao);
        using var comando = conexao.CreateCommand();
        comando.CommandText = $@"
            SELECT {ColunasEmprestimo}, l.Titulo, m.Nome
            FROM dbo.Emprestimos AS e
            INNER JOIN dbo.Livros AS l ON l.Id = e.LivroId
            INNER JOIN dbo.Membros AS m ON m.Id = e.MembroId
            WHERE e.DataDevolucao IS NULL
              {(somenteAtrasados ? "AND e.DataPrevistaDevolucao < @Hoje" : string.Empty)}
            ORDER BY e.DataPrevistaDevolucao, e.Id";
        if (somenteAtrasados)
        {
            comando.Parametro("@Hoje", SqlDbType.Date, hoje!.Value.ParaDateTime());
        }

        var lista = new List<EmprestimoDetalhado>();
        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
        {
            lista.Add(new EmprestimoDetalhado(LerEmprestimo(leitor), leitor.GetString(7), leitor.GetString(8)));
        }

        return lista;
    }

    private static Emprestimo LerEmprestimo(SqlDataReader leitor) =>
        new(leitor.GetInt32(0), leitor.GetInt32(1), leitor.GetInt32(2),
            leitor.LerData(3), leitor.LerData(4),
            leitor.IsDBNull(5) ? null : leitor.LerData(5),
            leitor.GetDecimal(6));
}
