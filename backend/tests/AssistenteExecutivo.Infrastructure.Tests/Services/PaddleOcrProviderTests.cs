using AssistenteExecutivo.Infrastructure.Services;
using FluentAssertions;

namespace AssistenteExecutivo.Infrastructure.Tests.Services;

public class PaddleOcrProviderTests
{
    [Theory]
    [InlineData("+55 11 93757-5221", "11937575221")]
    [InlineData("55 11 93757 5221", "11937575221")]
    [InlineData("(11) 93757-5221", "11937575221")]
    [InlineData("11 93757-5221", "11937575221")]
    [InlineData("11 3757-5221", "1137575221")]
    public void NormalizeBrazilPhone_ForDomain_ShouldReturnDigits(string input, string expected)
    {
        // Access via reflection because the helper is private (we only want regression coverage).
        var method = typeof(PaddleOcrProvider).GetMethod(
            "NormalizeBrazilPhoneForDomain",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.Should().NotBeNull();
        var result = (string?)method!.Invoke(null, new object?[] { input });
        result.Should().Be(expected);
    }
}

