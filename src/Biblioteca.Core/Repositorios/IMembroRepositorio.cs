using Biblioteca.Core.Modelos;

namespace Biblioteca.Core.Repositorios;

public interface IMembroRepositorio
{
    /// <summary>Salva o membro e devolve o Id gerado.</summary>
    int Adicionar(Membro membro);

    Membro? ObterPorId(int id);

    bool EmailExiste(string email);

    IReadOnlyList<Membro> Listar();
}
