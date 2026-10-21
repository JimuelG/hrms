using Core.Common;

namespace Tests;

public class IdentifierRulesTests
{
    [Theory]
    [InlineData(IdentifierField.Tin, "123-456-789", "123456789")]
    [InlineData(IdentifierField.Tin, "123 456 789 000", "123456789000")]
    [InlineData(IdentifierField.Sss, "12-3456789-0", "1234567890")]
    [InlineData(IdentifierField.PhilHealth, "12-345678901-2", "123456789012")]
    [InlineData(IdentifierField.PagIbig, "1234-5678-9012", "123456789012")]
    [InlineData(IdentifierField.BankAccountNumber, "00123.4567", "001234567")]
    public void Separators_Are_Stripped_And_The_Digits_Kept(IdentifierField f, string raw, string expected)
    {
        var (value, error) = IdentifierRules.Check(f, raw);
        Assert.Null(error);
        Assert.Equal(expected, value);
    }

    [Fact] public void Leading_Zeros_Survive() =>
        Assert.Equal("0123456789", IdentifierRules.Check(IdentifierField.Sss, "0123456789").Value);

    [Theory]
    [InlineData(IdentifierField.Tin, "12345678")]
    [InlineData(IdentifierField.Tin, "1234567890")]
    [InlineData(IdentifierField.Sss, "123456789")]
    [InlineData(IdentifierField.PhilHealth, "12345678901")]
    [InlineData(IdentifierField.PagIbig, "1234567890123")]
    [InlineData(IdentifierField.BankAccountNumber, "1234")]
    [InlineData(IdentifierField.BankAccountNumber, "123456789012345678901")]
    public void Wrong_Lengths_Are_Refused_With_The_Expected_Length(IdentifierField f, string raw) =>
        Assert.Contains("digits", IdentifierRules.Check(f, raw).Error);

    [Fact] public void Letters_Are_Refused() =>
        Assert.Contains("digits only", IdentifierRules.Check(IdentifierField.Sss, "12345ABC90").Error);

    [Fact] public void Blank_Input_Is_Refused() =>
        Assert.NotNull(IdentifierRules.Check(IdentifierField.Tin, "  - ").Error);

    [Fact] public void The_Error_Names_The_Field_And_The_Length() =>
        Assert.Equal("TIN should have 9 or 12 digits.", IdentifierRules.Check(IdentifierField.Tin, "1234").Error);

    [Theory]
    [InlineData("1234567890", "******7890")]
    [InlineData("12345", "*2345")]
    [InlineData("1234", "****")]
    [InlineData("12", "**")]
    public void Mask_Shows_Only_The_Last_Four(string value, string expected) =>
        Assert.Equal(expected, IdentifierRules.Mask(value));

    [Fact] public void Nothing_Masks_To_Nothing()
    {
        Assert.Null(IdentifierRules.Mask(null));
        Assert.Null(IdentifierRules.Mask(""));
    }
}