namespace Biblioteca.Core.Modelos;

/// <summary>Funções para trabalhar com ISBN-13.</summary>
public static class CodigoIsbn
{
    /// <summary>Remove hífens e espaços: "978-0-13-235088-4" vira "9780132350884".</summary>
    public static string Normalizar(string? valor) =>
        (valor ?? string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty).Trim();

    /// <summary>
    /// Confere se o ISBN (já normalizado) tem 13 dígitos e o dígito verificador correto.
    /// Os dígitos são somados com peso 1 e 3, alternadamente; o total precisa ser múltiplo de 10.
    /// </summary>
    public static bool EhValido(string isbn)
    {
        if (isbn.Length != 13 || !isbn.All(char.IsAsciiDigit))
        {
            return false;
        }

        var soma = 0;
        for (var i = 0; i < 13; i++)
        {
            var digito = isbn[i] - '0';
            soma += i % 2 == 0 ? digito : digito * 3;
        }

        return soma % 10 == 0;
    }
}
