using ClinicApp.Web.Services;
using Xunit;

namespace ClinicApp.Tests.Services;

public class PhoneNumberHelperTests
{
    // ── Valid Israeli numbers ──────────────────────────────────────

    [Theory]
    [InlineData("0507773344",   "972507773344")]   // local format
    [InlineData("05-077-73344", "972507773344")]   // dashes
    [InlineData("050 777 3344", "972507773344")]   // spaces
    [InlineData("+972507773344","972507773344")]   // international with +
    [InlineData("972507773344", "972507773344")]   // already normalized
    [InlineData("(050)7773344", "972507773344")]   // brackets
    public void Format_ValidIsraeliNumbers_ReturnsNormalized(string input, string expected)
    {
        var (isValid, formatted, error) = PhoneNumberHelper.Format(input);

        Assert.True(isValid, $"Expected valid but got error: {error}");
        Assert.Equal(expected, formatted);
        Assert.Null(error);
    }

    // ── Invalid / missing inputs ───────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Format_NullOrEmpty_ReturnsInvalid(string? input)
    {
        var (isValid, formatted, error) = PhoneNumberHelper.Format(input);

        Assert.False(isValid);
        Assert.Equal(string.Empty, formatted);
        Assert.NotNull(error);
    }

    [Fact]
    public void Format_NoDigits_ReturnsInvalid()
    {
        var (isValid, _, error) = PhoneNumberHelper.Format("---");

        Assert.False(isValid);
        Assert.NotNull(error);
    }

    [Fact]
    public void Format_UnrecognizedCountryCode_ReturnsInvalid()
    {
        // US number — not in the current rules
        var (isValid, _, error) = PhoneNumberHelper.Format("12025550123");

        Assert.False(isValid);
        Assert.NotNull(error);
    }

    [Fact]
    public void Format_TooShort_ReturnsInvalid()
    {
        var (isValid, _, error) = PhoneNumberHelper.Format("0501");

        Assert.False(isValid);
        Assert.NotNull(error);
    }
}
