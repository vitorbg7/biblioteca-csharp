using System.Data;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Data;

/// <summary>Funções de apoio para falar com o SQL Server.</summary>
public static class ConexaoSql
{
    /// <summary>
    /// Conexão padrão: o SQL Server LocalDB que acompanha o Visual Studio, com o login do Windows.
    /// Para usar outro servidor, defina a variável de ambiente BIBLIOTECA_CONNECTION.
    /// </summary>
    public const string ConexaoPadrao =
        @"Server=(localdb)\MSSQLLocalDB;Database=Biblioteca;Integrated Security=true;TrustServerCertificate=true";

    public static string ObterTextoDeConexao() =>
        Environment.GetEnvironmentVariable("BIBLIOTECA_CONNECTION") is { Length: > 0 } texto ? texto : ConexaoPadrao;

    /// <summary>Abre e fecha uma conexão. Lança SqlException se o servidor ou o banco não estiverem acessíveis.</summary>
    public static void Testar(string textoDeConexao)
    {
        using var conexao = new SqlConnection(textoDeConexao);
        conexao.Open();
    }

    /// <summary>Abre uma conexão nova. Quem chamar deve descartá-la (using).</summary>
    internal static SqlConnection Abrir(string textoDeConexao)
    {
        var conexao = new SqlConnection(textoDeConexao);
        conexao.Open();
        return conexao;
    }

    /// <summary>
    /// Adiciona um parâmetro tipado ao comando. Parâmetros (@Nome) evitam SQL injection:
    /// o valor nunca é colado no texto do comando.
    /// </summary>
    internal static void Parametro(this SqlCommand comando, string nome, SqlDbType tipo, object? valor)
    {
        comando.Parameters.Add(nome, tipo).Value = valor ?? DBNull.Value;
    }

    /// <summary>
    /// Parâmetro de dinheiro. Precisão e escala precisam ser informadas (DECIMAL(10,2) no banco);
    /// sem isso o driver pode descartar os centavos.
    /// </summary>
    internal static void ParametroMoeda(this SqlCommand comando, string nome, decimal valor)
    {
        var parametro = comando.Parameters.Add(nome, SqlDbType.Decimal);
        parametro.Precision = 10;
        parametro.Scale = 2;
        parametro.Value = valor;
    }

    /// <summary>O driver trata DATE como DateTime; convertemos de e para DateOnly nas bordas.</summary>
    internal static DateTime ParaDateTime(this DateOnly data) => data.ToDateTime(TimeOnly.MinValue);

    internal static DateOnly LerData(this SqlDataReader leitor, int coluna) =>
        DateOnly.FromDateTime(leitor.GetDateTime(coluna));
}
