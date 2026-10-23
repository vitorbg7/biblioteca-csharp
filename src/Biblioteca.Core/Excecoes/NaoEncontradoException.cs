namespace Biblioteca.Core.Excecoes;

/// <summary>Lançada quando um registro procurado pelo Id não existe.</summary>
public sealed class NaoEncontradoException : RegraDeNegocioException
{
    public NaoEncontradoException(string entidade, int id)
        : base($"{entidade} com código {id} não foi encontrado.")
    {
    }
}
