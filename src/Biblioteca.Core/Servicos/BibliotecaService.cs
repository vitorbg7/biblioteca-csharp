using Biblioteca.Core.Excecoes;
using Biblioteca.Core.Modelos;
using Biblioteca.Core.Repositorios;

namespace Biblioteca.Core.Servicos;

/// <summary>
/// Regras de negócio da biblioteca. Não sabe se os dados estão no SQL Server ou na memória:
/// só conversa com as interfaces de repositório. O relógio também é recebido de fora (TimeProvider),
/// o que permite testar situações como "o livro está 3 dias atrasado" sem esperar 3 dias.
/// </summary>
public sealed class BibliotecaService
{
    public const int LimiteDeEmprestimosPorMembro = 3;

    private readonly ILivroRepositorio _livros;
    private readonly IMembroRepositorio _membros;
    private readonly IEmprestimoRepositorio _emprestimos;
    private readonly TimeProvider _relogio;

    public BibliotecaService(ILivroRepositorio livros, IMembroRepositorio membros,
        IEmprestimoRepositorio emprestimos, TimeProvider relogio)
    {
        _livros = livros;
        _membros = membros;
        _emprestimos = emprestimos;
        _relogio = relogio;
    }

    public DateOnly Hoje() => DateOnly.FromDateTime(_relogio.GetLocalNow().DateTime);

    // ---------- Cadastros ----------

    public Livro CadastrarLivro(string titulo, string autor, string isbn, int anoPublicacao, int quantidadeTotal)
    {
        var livro = Livro.Novo(titulo, autor, isbn, anoPublicacao, quantidadeTotal);

        if (_livros.IsbnExiste(livro.Isbn))
            throw new RegraDeNegocioException($"Já existe um livro cadastrado com o ISBN {livro.Isbn}.");

        return livro.ComId(_livros.Adicionar(livro));
    }

    public Membro CadastrarMembro(string nome, string email)
    {
        var membro = Membro.Novo(nome, email, Hoje());

        if (_membros.EmailExiste(membro.Email))
            throw new RegraDeNegocioException($"Já existe um membro cadastrado com o e-mail {membro.Email}.");

        return membro.ComId(_membros.Adicionar(membro));
    }

    // ---------- Empréstimos ----------

    public Emprestimo Emprestar(int livroId, int membroId)
    {
        var livro = _livros.ObterPorId(livroId) ?? throw new NaoEncontradoException("Livro", livroId);
        var membro = _membros.ObterPorId(membroId) ?? throw new NaoEncontradoException("Membro", membroId);
        var hoje = Hoje();

        if (_emprestimos.MembroTemAtraso(membro.Id, hoje))
            throw new RegraDeNegocioException($"{membro.Nome} tem livros em atraso. Registre a devolução antes de emprestar outro.");

        if (_emprestimos.ContarEmAndamentoDoMembro(membro.Id) >= LimiteDeEmprestimosPorMembro)
            throw new RegraDeNegocioException($"{membro.Nome} já está com {LimiteDeEmprestimosPorMembro} livros emprestados, que é o limite.");

        var novo = Emprestimo.Novo(livro.Id, membro.Id, hoje);

        // A conferência final de exemplares acontece junto da gravação, de forma atômica.
        var id = _emprestimos.TentarRegistrar(novo)
                 ?? throw new RegraDeNegocioException($"Não há exemplar disponível de \"{livro.Titulo}\" no momento.");

        return novo.ComId(id);
    }

    public Emprestimo Devolver(int emprestimoId)
    {
        var emprestimo = _emprestimos.ObterPorId(emprestimoId) ?? throw new NaoEncontradoException("Empréstimo", emprestimoId);

        var devolvido = emprestimo.Devolver(Hoje());
        _emprestimos.RegistrarDevolucao(devolvido);
        return devolvido;
    }

    // ---------- Consultas ----------

    public IReadOnlyList<LivroComDisponibilidade> ListarLivros(string? termo = null) => _livros.Listar(termo);

    public IReadOnlyList<Membro> ListarMembros() => _membros.Listar();

    public IReadOnlyList<EmprestimoDetalhado> EmprestimosEmAndamento() => _emprestimos.ListarEmAndamento();

    public IReadOnlyList<EmprestimoDetalhado> EmprestimosAtrasados() => _emprestimos.ListarAtrasados(Hoje());

    public IReadOnlyList<LivroMaisEmprestado> LivrosMaisEmprestados(int limite = 5) => _livros.MaisEmprestados(limite);
}
