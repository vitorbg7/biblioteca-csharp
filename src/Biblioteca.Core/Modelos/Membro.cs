using System.Net.Mail;
using Biblioteca.Core.Excecoes;

namespace Biblioteca.Core.Modelos;

/// <summary>Pessoa cadastrada na biblioteca, identificada pelo e-mail (único).</summary>
public sealed class Membro
{
    public const int NomeMaximo = 100;
    public const int EmailMaximo = 150;

    public Membro(int id, string nome, string email, DateOnly dataCadastro)
    {
        nome = (nome ?? string.Empty).Trim();
        email = (email ?? string.Empty).Trim().ToLowerInvariant();

        if (nome.Length == 0) throw new RegraDeNegocioException("Informe o nome do membro.");
        if (nome.Length > NomeMaximo) throw new RegraDeNegocioException($"O nome pode ter até {NomeMaximo} caracteres.");
        if (email.Length > EmailMaximo || !EmailValido(email)) throw new RegraDeNegocioException("E-mail inválido. Use o formato nome@dominio.com.");

        Id = id;
        Nome = nome;
        Email = email;
        DataCadastro = dataCadastro;
    }

    public int Id { get; }
    public string Nome { get; }
    public string Email { get; }
    public DateOnly DataCadastro { get; }

    public static Membro Novo(string nome, string email, DateOnly hoje) => new(0, nome, email, hoje);

    public Membro ComId(int id) => new(id, Nome, Email, DataCadastro);

    // O MailAddress também aceita formatos como "Ana <ana@x.com>". Comparar o endereço com o texto
    // original garante que só passa o e-mail "puro".
    private static bool EmailValido(string email) =>
        MailAddress.TryCreate(email, out var endereco) && endereco.Address == email && endereco.Host.Contains('.');
}
