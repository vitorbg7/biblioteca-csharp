using System.Globalization;
using Biblioteca.Core.Excecoes;
using Biblioteca.Core.Modelos;
using Biblioteca.Core.Servicos;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Cli;

/// <summary>Menu de console. Só lê o que a pessoa digita e mostra os resultados; as regras ficam no BibliotecaService.</summary>
public sealed class Aplicacao
{
    private static readonly CultureInfo Br = new("pt-BR");

    private readonly BibliotecaService _servico;

    public Aplicacao(BibliotecaService servico) => _servico = servico;

    public void Executar()
    {
        while (true)
        {
            MostrarMenu();
            try
            {
                switch (Entrada.LerInteiro("Escolha uma opção: "))
                {
                    case 1: CadastrarLivro(); break;
                    case 2: CadastrarMembro(); break;
                    case 3: ListarLivros(null); break;
                    case 4: ListarLivros(Entrada.LerTexto("Buscar por título ou autor: ")); break;
                    case 5: ListarMembros(); break;
                    case 6: Emprestar(); break;
                    case 7: Devolver(); break;
                    case 8: MostrarEmprestimos("Empréstimos em andamento", _servico.EmprestimosEmAndamento()); break;
                    case 9: MostrarEmprestimos("Empréstimos atrasados", _servico.EmprestimosAtrasados()); break;
                    case 10: MostrarRanking(); break;
                    case 0: Console.WriteLine("Até mais!"); return;
                    default: Console.WriteLine("Opção inválida."); break;
                }
            }
            catch (RegraDeNegocioException erro)
            {
                Console.WriteLine($"Erro: {erro.Message}");
            }
            catch (SqlException erro)
            {
                Console.WriteLine($"Erro no banco de dados: {erro.Message}");
            }
            catch (EntradaEncerradaException)
            {
                Console.WriteLine();
                return;
            }
        }
    }

    private static void MostrarMenu()
    {
        Console.WriteLine();
        Console.WriteLine("=== Biblioteca ===");
        Console.WriteLine(" 1 - Cadastrar livro");
        Console.WriteLine(" 2 - Cadastrar membro");
        Console.WriteLine(" 3 - Listar livros");
        Console.WriteLine(" 4 - Buscar livros");
        Console.WriteLine(" 5 - Listar membros");
        Console.WriteLine(" 6 - Registrar empréstimo");
        Console.WriteLine(" 7 - Registrar devolução");
        Console.WriteLine(" 8 - Empréstimos em andamento");
        Console.WriteLine(" 9 - Empréstimos atrasados");
        Console.WriteLine("10 - Livros mais emprestados");
        Console.WriteLine(" 0 - Sair");
    }

    // ---------- Ações ----------

    private void CadastrarLivro()
    {
        var titulo = Entrada.LerTexto("Título: ");
        var autor = Entrada.LerTexto("Autor: ");
        var isbn = Entrada.LerTexto("ISBN (13 dígitos): ");
        var ano = Entrada.LerInteiro("Ano de publicação: ");
        var quantidade = Entrada.LerInteiro("Quantidade de exemplares: ");

        var livro = _servico.CadastrarLivro(titulo, autor, isbn, ano, quantidade);
        Console.WriteLine($"Livro cadastrado com o código {livro.Id}.");
    }

    private void CadastrarMembro()
    {
        var nome = Entrada.LerTexto("Nome: ");
        var email = Entrada.LerTexto("E-mail: ");

        var membro = _servico.CadastrarMembro(nome, email);
        Console.WriteLine($"Membro cadastrado com o código {membro.Id}.");
    }

    private void ListarLivros(string? termo)
    {
        var livros = _servico.ListarLivros(termo);
        if (livros.Count == 0)
        {
            Console.WriteLine(termo is null ? "Nenhum livro cadastrado." : "Nenhum livro encontrado para essa busca.");
            return;
        }

        Console.WriteLine($"{"Cód.",4}  {"Título",-34} {"Autor",-20} {"Ano",4}  Disponíveis");
        foreach (var item in livros)
        {
            var livro = item.Livro;
            Console.WriteLine(
                $"{livro.Id,4}  {Cortar(livro.Titulo, 34),-34} {Cortar(livro.Autor, 20),-20} {livro.AnoPublicacao,4}  {item.Disponiveis} de {livro.QuantidadeTotal}");
        }
    }

    private void ListarMembros()
    {
        var membros = _servico.ListarMembros();
        if (membros.Count == 0)
        {
            Console.WriteLine("Nenhum membro cadastrado.");
            return;
        }

        Console.WriteLine($"{"Cód.",4}  {"Nome",-24} {"E-mail",-32} Desde");
        foreach (var membro in membros)
        {
            Console.WriteLine(
                $"{membro.Id,4}  {Cortar(membro.Nome, 24),-24} {Cortar(membro.Email, 32),-32} {membro.DataCadastro:dd/MM/yyyy}");
        }
    }

    private void Emprestar()
    {
        var livroId = Entrada.LerInteiro("Código do livro: ");
        var membroId = Entrada.LerInteiro("Código do membro: ");

        var emprestimo = _servico.Emprestar(livroId, membroId);
        Console.WriteLine(
            $"Empréstimo {emprestimo.Id} registrado. Devolução prevista para {emprestimo.DataPrevistaDevolucao:dd/MM/yyyy}.");
    }

    private void Devolver()
    {
        var emprestimoId = Entrada.LerInteiro("Código do empréstimo: ");

        var devolvido = _servico.Devolver(emprestimoId);
        Console.WriteLine(devolvido.Multa > 0
            ? $"Devolução registrada com {devolvido.DiasDeAtraso(_servico.Hoje())} dia(s) de atraso. Multa: {devolvido.Multa.ToString("C", Br)}."
            : "Devolução registrada. Sem multa.");
    }

    private void MostrarEmprestimos(string titulo, IReadOnlyList<EmprestimoDetalhado> lista)
    {
        Console.WriteLine(titulo);
        if (lista.Count == 0)
        {
            Console.WriteLine("Nenhum registro.");
            return;
        }

        var hoje = _servico.Hoje();
        Console.WriteLine($"{"Cód.",4}  {"Livro",-30} {"Membro",-18} {"Prazo",-10}  Situação");
        foreach (var item in lista)
        {
            var e = item.Emprestimo;
            var situacao = e.EstaAtrasado(hoje)
                ? $"atrasado há {e.DiasDeAtraso(hoje)} dia(s), multa {e.CalcularMulta(hoje).ToString("C", Br)}"
                : "no prazo";
            Console.WriteLine(
                $"{e.Id,4}  {Cortar(item.TituloLivro, 30),-30} {Cortar(item.NomeMembro, 18),-18} {e.DataPrevistaDevolucao:dd/MM/yyyy}  {situacao}");
        }
    }

    private void MostrarRanking()
    {
        var ranking = _servico.LivrosMaisEmprestados(5);
        if (ranking.Count == 0)
        {
            Console.WriteLine("Ainda não há empréstimos registrados.");
            return;
        }

        Console.WriteLine("Livros mais emprestados");
        var posicao = 1;
        foreach (var item in ranking)
        {
            Console.WriteLine($"{posicao++,2}. {item.Titulo} ({item.Autor}): {item.TotalEmprestimos} empréstimo(s)");
        }
    }

    private static string Cortar(string texto, int tamanho) =>
        texto.Length <= tamanho ? texto : texto[..(tamanho - 1)] + "…";
}
