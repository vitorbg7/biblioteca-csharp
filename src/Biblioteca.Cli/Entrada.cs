namespace Biblioteca.Cli;

/// <summary>Lançada quando a entrada do teclado acaba (Ctrl+Z / Ctrl+D ou entrada redirecionada).</summary>
public sealed class EntradaEncerradaException : Exception
{
}

/// <summary>Leitura de dados digitados, repetindo a pergunta até receber algo utilizável.</summary>
public static class Entrada
{
    public static string LerTexto(string pergunta)
    {
        Console.Write(pergunta);
        return Console.ReadLine()?.Trim() ?? throw new EntradaEncerradaException();
    }

    public static int LerInteiro(string pergunta)
    {
        while (true)
        {
            if (int.TryParse(LerTexto(pergunta), out var numero))
            {
                return numero;
            }

            Console.WriteLine("Digite um número inteiro.");
        }
    }
}
