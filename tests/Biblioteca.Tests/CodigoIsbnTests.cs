using Biblioteca.Core.Modelos;
using Xunit;

namespace Biblioteca.Tests;

public class CodigoIsbnTests
{
    [Theory]
    [InlineData("9780132350884")]
    [InlineData("978-0-13-235088-4")]
    [InlineData("978 0 201 63361 0")]
    public void AceitaIsbnComDigitoVerificadorCorreto(string entrada)
    {
        Assert.True(CodigoIsbn.EhValido(CodigoIsbn.Normalizar(entrada)));
    }

    [Theory]
    [InlineData("9780132350885")]   // dígito verificador errado
    [InlineData("978013235088")]    // 12 dígitos
    [InlineData("97801323508844")]  // 14 dígitos
    [InlineData("97801323508AB")]   // letras
    [InlineData("")]
    public void RejeitaIsbnInvalido(string entrada)
    {
        Assert.False(CodigoIsbn.EhValido(CodigoIsbn.Normalizar(entrada)));
    }

    [Fact]
    public void NormalizarRemoveHifensEEspacos()
    {
        Assert.Equal("9780132350884", CodigoIsbn.Normalizar(" 978-0-13 235088-4 "));
    }
}
