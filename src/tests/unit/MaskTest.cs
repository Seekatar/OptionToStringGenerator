using Microsoft.CodeAnalysis;
using Shouldly;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Seekatar.OptionToStringGenerator.Tests;


public class MaskTests
{
    [Theory]
    [InlineData(null, false, OptionsToStringAttribute.NullLiteral)]
    [InlineData("", false, "Len = 0")]
    [InlineData("test", false, "Len = 4")]
    [InlineData(null, true, OptionsToStringAttribute.NullLiteral)]
    [InlineData("", true, "{ \"Len\": 0 }")]
    [InlineData("test", true, "{ \"Len\": 4 }")]
    public void FormatLengthOnly_MultipleInputs_ReturnsExpectedResults(object? input, bool asJson, string? expected)
    {
        // call old one to ensure backwards compatibility not broken
        // Act
#pragma warning disable CS0618 // Type or member is obsolete
        var result = OptionsToStringAttribute.Format(input, lengthOnly: true, asJson: asJson);
#pragma warning restore CS0618 // Type or member is obsolete

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, false, null)]
    [InlineData("", false, "Len = 0")]
    [InlineData("test", false, "Len = 4")]
    [InlineData(null, true, null)]
    [InlineData("", true, "{ \"Len\": 0 }")]
    [InlineData("test", true, "{ \"Len\": 4 }")]
    public void MaskLengthOnly_MultipleInputs_ReturnsExpectedResults(object? input, bool asJson, string? expected)
    {
        // Act
        var result = Mask.MaskLengthOnly(input, asJson);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, 2, '*', false, null)]
    [InlineData("", 2, '*', false, "")]
    [InlineData("test", 2, '*', false, "**st")]
    [InlineData("test", 0, '*', false, "****")]
    [InlineData("test", 2, '.', false, "..st")]
    [InlineData("test", 5, '*', false, "test")]
    [InlineData(null, 2, '*', true, null)]
    [InlineData("", 2, '*', true, "\"\"")]
    [InlineData("test", 2, '*', true, "\"**st\"")]
    public void MaskPrefix_MultipleInputs_ReturnsExpectedResults(object? input, int suffixLen, char maskChar, bool asJson, string? expected)
    {
        // Act
        var result = Mask.MaskPrefix(input, suffixLen, maskChar, asJson);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("test", -1, '*', false, "prefixLen and suffixLen cannot both be less than zero")]
    public void MaskPrefix_NegativeInputs_ThrowsException(object? input, int suffixLen, char maskChar, bool asJson, string? expectedException)
    {
        // Act and Assert
        if (expectedException != null)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Mask.MaskPrefix(input, suffixLen, maskChar, asJson));
            exception.ParamName.ShouldBe(expectedException);
        }
        else
        {
            Should.NotThrow(() => Mask.MaskPrefix(input, suffixLen, maskChar, asJson));
        }
    }

    [Theory]
    [InlineData(null, 2, '*', false, null)]
    [InlineData("", 2, '*', false, "")]
    [InlineData("test", 2, '*', false, "te**")]
    [InlineData("test", 4, '*', false, "test")]
    [InlineData("test", 0, '*', false, "****")]
    [InlineData("test", 2, '.', false, "te..")]
    [InlineData(null, 2, '*', true, null)]
    [InlineData("", 2, '*', true, "\"\"")]
    [InlineData("test", 2, '*', true, "\"te**\"")]
    public void MaskSuffix_MultipleInputs_ReturnsExpectedResults(object? input, int prefixLen, char maskChar, bool asJson, string? expected)
    {
        // Act
        var result = Mask.MaskSuffix(input, prefixLen, maskChar, asJson);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("test", -1, '*', false, "prefixLen and suffixLen cannot both be less than zero")]
    public void MaskSuffix_NegativeInputs_ThrowsException(object? input, int prefixLen, char maskChar, bool asJson, string? expectedException)
    {
        // Act and Assert
        if (expectedException != null)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Mask.MaskSuffix(input, prefixLen, maskChar, asJson));
            exception.ParamName.ShouldBe(expectedException);
        }
        else
        {
            Should.NotThrow(() => Mask.MaskPrefix(input, prefixLen, maskChar, asJson));
        }
    }



    [Theory]
    [InlineData(null, 2, 2, '*', false, null)]
    [InlineData("", 2, 2, '*', false, "")]
    [InlineData("testing", 2, 2, '*', false, "te***ng")]
    [InlineData("testing", 5, 2, '*', false, "testing")]
    [InlineData("testing", 0, 15, '*', false, "testing")]
    [InlineData("testing", 2, 2, '.', false, "te...ng")]
    [InlineData(null, 2, 2, '*', true, null)]
    [InlineData("", 2, 2, '*', true, "\"\"")]
    [InlineData("testing", 2, 2, '*', true, "\"te***ng\"")]
    public void MaskPrefixSuffix_MultipleInputs_ReturnsExpectedResults(object? input, int prefixLen, int suffixLen, char maskChar, bool asJson, string? expected)
    {
        // Act
        var result = Mask.MaskPrefixSuffix(input, prefixLen, suffixLen, maskChar, asJson);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("test", -1, -1, '*', false, "prefixLen and suffixLen cannot both be less than zero")]
    public void MaskPrefixSuffix_NegativeInputs_ThrowsException(object? input, int prefixLen, int suffixLen, char maskChar, bool asJson, string? expectedException)
    {
        // Act and Assert
        if (expectedException != null)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Mask.MaskPrefixSuffix(input, prefixLen, suffixLen, maskChar, asJson));
            exception.ParamName.ShouldBe(expectedException);
        }
        else
        {
            Should.NotThrow(() => Mask.MaskPrefix(input, prefixLen, maskChar, asJson));
        }
    }

    [Theory]
    [InlineData(null, '*', false, null)]
    [InlineData("", '*', false, "")]
    [InlineData("test", '*', false, "****")]
    [InlineData(null, '*', true, null)]
    [InlineData("", '*', true, "\"\"")]
    [InlineData("test", '*', true, "\"****\"")]
    public void MaskAll_MultipleInputs_ReturnsExpectedResults(object? input, char maskChar, bool asJson, string? expected)
    {
        // Act
        var result = Mask.MaskAll(input, maskChar, asJson);

        // Assert
        result.ShouldBe(expected);
    }

    /*
     You have a problem you decide to solve with a regex. Now you have two problems. Here's what CoPilot says about the TestRegex:

    (.*?): This is a capturing group that matches any character (.) any number of times (*?). The ? makes the * quantifier non-greedy, meaning it will match as few characters as possible.

    (?<!\\);: This is a positive lookbehind assertion that matches a semicolon (;) that is not preceded by a backslash (\\). The (?<!...) syntax defines a negative lookbehind assertion, which means the regex engine will only consider a match valid if the pattern inside the lookbehind is not present before the current position.
     */

    const string TestRegexString = @"Server=localhost;User Id=admin;Password=`~!@#$%^&*()_+=-\\][{}|'\;:"",./?><;Packet Size = 4096;";
    const string TestRegex = @"Password=(.*?)(?<!\\);";
    [Theory]
    [InlineData(null, TestRegex, false, false, null)]
    [InlineData("", TestRegex, false, false, "***Regex no match***!")]
    [InlineData(TestRegexString, TestRegex, false, false, "Server=localhost;User Id=admin;Password=***;Packet Size = 4096;")]
    [InlineData(TestRegexString, TestRegex, true, false, "Server=localhost;User Id=admin;Password=***;Packet Size = 4096;")]
    [InlineData(TestRegexString, TestRegex, false, true, "\"Server=localhost;User Id=admin;Password=***;Packet Size = 4096;\"")]
    [InlineData(TestRegexString, TestRegex, true, true, "\"Server=localhost;User Id=admin;Password=***;Packet Size = 4096;\"")]
    [InlineData(TestRegexString, @"User Id=(\w+);Password=(.*?)(?<!\\);", true, true, "\"Server=localhost;User Id=***;Password=***;Packet Size = 4096;\"")]
    public void MaskRegex_MultipleInputs_ReturnsExpectedResults(object? input, string regex, bool ignoreCase, bool asJson, string? expected)
    {
        // Act
        var result = Mask.MaskRegex(input, regex, ignoreCase, asJson);

        // Assert
        result.ShouldBe(expected);
    }

    public static string MaskFunc(string input) => input.Replace(input, new string('#', input.Length));

    [Theory]
    [InlineData(TestRegexString, TestRegex, false, false, "Server=localhost;User Id=admin;Password=##################################;Packet Size = 4096;")]
    [InlineData(TestRegexString, TestRegex, true, false, "Server=localhost;User Id=admin;Password=##################################;Packet Size = 4096;")]
    [InlineData(TestRegexString, TestRegex, false, true, "\"Server=localhost;User Id=admin;Password=##################################;Packet Size = 4096;\"")]
    [InlineData(TestRegexString, TestRegex, true, true, "\"Server=localhost;User Id=admin;Password=##################################;Packet Size = 4096;\"")]
    public void MaskRegexWithFunc_MultipleInputs_ReturnsExpectedResults(object? input, string regex, bool ignoreCase, bool asJson, string expected)
    {
        // Act
        var result = Mask.MaskRegex(input, regex, ignoreCase, asJson, MaskFunc);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(TestRegexString, "Server=([^;]+;Password=(.*", true, true, "Invalid pattern 'Server=([^;]+;Password=(.*' at offset 26. Not enough )'s.")]
    public void MaskRegex_NegativeInputs_ThrowsException(object? input, string regex, bool ignoreCase, bool asJson, string expectedException)
    {
        // Act and Assert
        var exception = Assert.Throws<RegexParseException>(() => Mask.MaskRegex(input, regex, ignoreCase, asJson, MaskFunc));
        exception.Message.ShouldBe(expectedException);
    }

    [Theory]
    [InlineData(null, false, -1, -1, null, false, false, '*', "null")]
    [InlineData("", false, -1, -1, null, false, false, '*', "\"\"")]
    [InlineData("test", false, -1, -1, null, false, false, '*', "\"test\"")]
    [InlineData("test", true, -1, -1, null, false, false, '*', "Len = 4")]
    [InlineData("testing", false, 2, 2, null, false, false, '*', "\"te***ng\"")]
    [InlineData("test", false, -1, -1, "(e)", false, false, '*', "\"t***st\"")]
    [InlineData(true, false, -1, -1, null, false, false, '*', "true")]
    [InlineData(123, false, -1, -1, null, false, false, '*', "123")]
    public void Format_MultipleInputs_ReturnsExpectedResults(object? input, bool lengthOnly, int prefixLen, int suffixLen, string? regex, bool ignoreCase, bool asJson, char maskChar, string? expected)
    {
        // Act
        var result = Mask.Format(input, lengthOnly, prefixLen, suffixLen, regex, ignoreCase, asJson, maskChar);

        // Assert
        result.ShouldBe(expected);
    }
}