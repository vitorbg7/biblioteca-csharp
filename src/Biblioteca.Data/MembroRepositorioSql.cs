using System.Data;
using Biblioteca.Core.Modelos;
using Biblioteca.Core.Repositorios;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Data;

public sealed class MembroRepositorioSql : IMembroRepositorio
{
    private readonly string _conexao;

    public MembroRepositorioSql(string textoDeConexao) => _conexao = textoDeConexao;

    public int Adicionar(Membro membro)
    {
        using var conexao = ConexaoSql.Abrir(_conexao);
        using var comando = conexao.CreateCommand();
        comando.CommandText = @"
            INSERT INTO dbo.Membros (Nome, Email, DataCadastro)
            OUTPUT INSERTED.Id
            VALUES (@Nome, @Email, @DataCadastro)";
        comando.Parametro("@Nome", SqlDbType.NVarChar, membro.Nome);
        comando.Parametro("@Email", SqlDbType.NVarChar, membro.Email);
        comando.Parametro("@DataCadastro", SqlDbType.Date, membro.DataCadastro.ParaDateTime());
        return (int)comando.ExecuteScalar()!;
    }

    public Membro? ObterPorId(int id)
    {
        using var conexao = ConexaoSql.Abrir(_conexao);
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT Id, Nome, Email, DataCadastro FROM dbo.Membros WHERE Id = @Id";
        comando.Parametro("@Id", SqlDbType.Int, id);

        using var leitor = comando.ExecuteReader();
        return leitor.Read() ? LerMembro(leitor) : null;
    }

    public bool EmailExiste(string email)
    {
        using var conexao = ConexaoSql.Abrir(_conexao);
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT COUNT(1) FROM dbo.Membros WHERE Email = @Email";
        comando.Parametro("@Email", SqlDbType.NVarChar, email);
        return (int)comando.ExecuteScalar()! > 0;
    }

    public IReadOnlyList<Membro> Listar()
    {
        using var conexao = ConexaoSql.Abrir(_conexao);
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT Id, Nome, Email, DataCadastro FROM dbo.Membros ORDER BY Nome";

        var membros = new List<Membro>();
        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
        {
            membros.Add(LerMembro(leitor));
        }

        return membros;
    }

    private static Membro LerMembro(SqlDataReader leitor) =>
        new(leitor.GetInt32(0), leitor.GetString(1), leitor.GetString(2), leitor.LerData(3));
}
