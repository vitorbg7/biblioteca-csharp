using Biblioteca.Core.Modelos;

namespace Biblioteca.Core.Repositorios;

public interface IEmprestimoRepositorio
{
    int ContarEmAndamentoDoMembro(int membroId);

    bool MembroTemAtraso(int membroId, DateOnly hoje);

    /// <summary>
    /// Registra o empréstimo SE ainda houver exemplar livre, e devolve o Id gerado; caso contrário devolve null.
    /// A conferência e a gravação acontecem de forma atômica, para que duas pessoas
    /// não peguem o último exemplar ao mesmo tempo.
    /// </summary>
    int? TentarRegistrar(Emprestimo emprestimo);

    Emprestimo? ObterPorId(int id);

    /// <summary>Grava a data de devolução e a multa de um empréstimo que estava em andamento.</summary>
    void RegistrarDevolucao(Emprestimo devolvido);

    IReadOnlyList<EmprestimoDetalhado> ListarEmAndamento();

    IReadOnlyList<EmprestimoDetalhado> ListarAtrasados(DateOnly hoje);
}
