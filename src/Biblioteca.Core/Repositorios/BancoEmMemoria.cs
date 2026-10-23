using System.Globalization;
using System.Text;
using Biblioteca.Core.Excecoes;
using Biblioteca.Core.Modelos;

namespace Biblioteca.Core.Repositorios;

/// <summary>
/// Implementação dos três repositórios que guarda tudo em listas na memória.
/// Serve para os testes automatizados e para o modo "--memoria" do programa, que roda sem SQL Server.
/// Uma instância só implementa as três interfaces porque as consultas precisam enxergar as três tabelas.
/// </summary>
public sealed class BancoEmMemoria : ILivroRepositorio, IMembroRepositorio, IEmprestimoRepositorio
{
    private readonly object _trava = new();
    private readonly List<Livro> _livros = new();
    private readonly List<Membro> _membros = new();
    private readonly List<Emprestimo> _emprestimos = new();
    private int _proximoIdLivro = 1;
    private int _proximoIdMembro = 1;
    private int _proximoIdEmprestimo = 1;

    // ---------- Livros ----------

    int ILivroRepositorio.Adicionar(Livro livro)
    {
        lock (_trava)
        {
            var id = _proximoIdLivro++;
            _livros.Add(livro.ComId(id));
            return id;
        }
    }

    Livro? ILivroRepositorio.ObterPorId(int id)
    {
        lock (_trava) return _livros.FirstOrDefault(l => l.Id == id);
    }

    bool ILivroRepositorio.IsbnExiste(string isbn)
    {
        lock (_trava) return _livros.Any(l => l.Isbn == isbn);
    }

    IReadOnlyList<LivroComDisponibilidade> ILivroRepositorio.Listar(string? termo)
    {
        lock (_trava)
        {
            var busca = SemAcentos(termo ?? string.Empty);
            return _livros
                .Where(l => busca.Length == 0
                            || SemAcentos(l.Titulo).Contains(busca)
                            || SemAcentos(l.Autor).Contains(busca))
                .OrderBy(l => l.Titulo, StringComparer.OrdinalIgnoreCase)
                .Select(l => new LivroComDisponibilidade(l, ContarEmAndamentoDoLivro(l.Id)))
                .ToList();
        }
    }

    IReadOnlyList<LivroMaisEmprestado> ILivroRepositorio.MaisEmprestados(int limite)
    {
        lock (_trava)
        {
            return _emprestimos
                .GroupBy(e => e.LivroId)
                .Select(g => new { Livro = _livros.First(l => l.Id == g.Key), Total = g.Count() })
                .OrderByDescending(x => x.Total)
                .ThenBy(x => x.Livro.Titulo, StringComparer.OrdinalIgnoreCase)
                .Take(limite)
                .Select(x => new LivroMaisEmprestado(x.Livro.Titulo, x.Livro.Autor, x.Total))
                .ToList();
        }
    }

    // ---------- Membros ----------

    int IMembroRepositorio.Adicionar(Membro membro)
    {
        lock (_trava)
        {
            var id = _proximoIdMembro++;
            _membros.Add(membro.ComId(id));
            return id;
        }
    }

    Membro? IMembroRepositorio.ObterPorId(int id)
    {
        lock (_trava) return _membros.FirstOrDefault(m => m.Id == id);
    }

    bool IMembroRepositorio.EmailExiste(string email)
    {
        lock (_trava) return _membros.Any(m => m.Email == email);
    }

    IReadOnlyList<Membro> IMembroRepositorio.Listar()
    {
        lock (_trava) return _membros.OrderBy(m => m.Nome, StringComparer.OrdinalIgnoreCase).ToList();
    }

    // ---------- Empréstimos ----------

    int IEmprestimoRepositorio.ContarEmAndamentoDoMembro(int membroId)
    {
        lock (_trava) return _emprestimos.Count(e => e.MembroId == membroId && e.EmAndamento);
    }

    bool IEmprestimoRepositorio.MembroTemAtraso(int membroId, DateOnly hoje)
    {
        lock (_trava) return _emprestimos.Any(e => e.MembroId == membroId && e.EstaAtrasado(hoje));
    }

    int? IEmprestimoRepositorio.TentarRegistrar(Emprestimo emprestimo)
    {
        lock (_trava)
        {
            var livro = _livros.FirstOrDefault(l => l.Id == emprestimo.LivroId);
            if (livro is null) return null;

            // Só ocupa exemplar quem ainda está em andamento (registros antigos já devolvidos não contam).
            if (emprestimo.EmAndamento && ContarEmAndamentoDoLivro(livro.Id) >= livro.QuantidadeTotal)
            {
                return null;
            }

            var id = _proximoIdEmprestimo++;
            _emprestimos.Add(emprestimo.ComId(id));
            return id;
        }
    }

    Emprestimo? IEmprestimoRepositorio.ObterPorId(int id)
    {
        lock (_trava) return _emprestimos.FirstOrDefault(e => e.Id == id);
    }

    void IEmprestimoRepositorio.RegistrarDevolucao(Emprestimo devolvido)
    {
        lock (_trava)
        {
            var indice = _emprestimos.FindIndex(e => e.Id == devolvido.Id && e.EmAndamento);
            if (indice < 0) throw new RegraDeNegocioException("Esse empréstimo já foi devolvido.");
            _emprestimos[indice] = devolvido;
        }
    }

    IReadOnlyList<EmprestimoDetalhado> IEmprestimoRepositorio.ListarEmAndamento()
    {
        lock (_trava)
        {
            return _emprestimos.Where(e => e.EmAndamento)
                .OrderBy(e => e.DataPrevistaDevolucao)
                .Select(Detalhar)
                .ToList();
        }
    }

    IReadOnlyList<EmprestimoDetalhado> IEmprestimoRepositorio.ListarAtrasados(DateOnly hoje)
    {
        lock (_trava)
        {
            return _emprestimos.Where(e => e.EstaAtrasado(hoje))
                .OrderBy(e => e.DataPrevistaDevolucao)
                .Select(Detalhar)
                .ToList();
        }
    }

    // ---------- Auxiliares (sempre chamados dentro do lock) ----------

    private int ContarEmAndamentoDoLivro(int livroId) =>
        _emprestimos.Count(e => e.LivroId == livroId && e.EmAndamento);

    private EmprestimoDetalhado Detalhar(Emprestimo e) =>
        new(e, _livros.First(l => l.Id == e.LivroId).Titulo, _membros.First(m => m.Id == e.MembroId).Nome);

    /// <summary>"Código Limpo" vira "codigo limpo", para a busca ignorar maiúsculas e acentos.</summary>
    private static string SemAcentos(string texto)
    {
        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);
        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }

        return sb.ToString();
    }
}
