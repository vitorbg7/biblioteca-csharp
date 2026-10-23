using System.Text;
using Biblioteca.Cli;
using Biblioteca.Core;
using Biblioteca.Core.Repositorios;
using Biblioteca.Core.Servicos;
using Biblioteca.Data;
using Microsoft.Data.SqlClient;

// UTF-8 para os acentos aparecerem certos no terminal do Windows.
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

BibliotecaService servico;

if (args.Contains("--memoria"))
{
    // Modo sem banco de dados: tudo fica na memória e some ao fechar o programa.
    var banco = new BancoEmMemoria();
    var hoje = DateOnly.FromDateTime(DateTime.Now);
    DadosDeExemplo.Carregar(banco, banco, banco, hoje);
    servico = new BibliotecaService(banco, banco, banco, TimeProvider.System);
    Console.WriteLine("Modo memória: dados de exemplo carregados, sem SQL Server.");
}
else
{
    var conexao = ConexaoSql.ObterTextoDeConexao();
    try
    {
        ConexaoSql.Testar(conexao);
    }
    catch (SqlException erro)
    {
        Console.WriteLine($"Não foi possível conectar ao SQL Server: {erro.Message}");
        Console.WriteLine("Confira se o servidor está rodando e se os scripts da pasta database foram executados.");
        Console.WriteLine("Para testar sem banco de dados, rode com: dotnet run --project src/Biblioteca.Cli -- --memoria");
        return 1;
    }

    servico = new BibliotecaService(
        new LivroRepositorioSql(conexao),
        new MembroRepositorioSql(conexao),
        new EmprestimoRepositorioSql(conexao),
        TimeProvider.System);
}

new Aplicacao(servico).Executar();
return 0;
