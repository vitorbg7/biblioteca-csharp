namespace Biblioteca.Core.Excecoes;

/// <summary>
/// Erro causado por uma regra do sistema (dado inválido, livro sem exemplares, limite excedido...).
/// A mensagem é escrita para o usuário final e pode ser exibida como está.
/// </summary>
public class RegraDeNegocioException : Exception
{
    public RegraDeNegocioException(string mensagem) : base(mensagem)
    {
    }
}
