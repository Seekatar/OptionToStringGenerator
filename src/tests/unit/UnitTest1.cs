//using VerifyXunit;

namespace Seekatar.OptionToStringGenerator.Tests;

[UsesVerify] // Adds hooks for Verify into XUnit
public class EnumGeneratorSnapshotTests
{
    [Fact]
    public Task GeneratesEnumExtensionsCorrectly()
    {
        // The source code to test
        var source = @"
using Seekatar.OptionToStringGenerator;

[OptionsToStringAttribute]
public class MyAppOptions
{
    public string Name { get; set; } = "";
}";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public void GeneratesEnumExtensions()
    {
        
    }
}