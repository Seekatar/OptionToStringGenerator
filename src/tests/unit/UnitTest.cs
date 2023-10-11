using Shouldly;

namespace Seekatar.OptionToStringGenerator.Tests;

[UsesVerify] // Adds hooks for Verify into XUnit
public class UnitTests
{
    [Theory]
    [InlineData("TestFiles/FormattingOptions.cs")]
    [InlineData("TestFiles/TitleOptions.cs")]
    [InlineData("TestFiles/JsonOptions.cs")]
    [InlineData("TestFiles/InternalOptions.cs")]
    [InlineData("TestFiles/PublicOptions.cs")]
    public Task HappyPathFiles(string filename)
    {
        // Pass the source code to our helper and snapshot test the output
        return TestHelper.VerifyFile(filename);
    }

    [Fact]
    public Task MultipleAttributesCauseWarning()
    {
        // The source code to test
        var source = @"
                        using Seekatar.OptionToStringGenerator;

                        [OptionsToStringAttribute]
                        public class MyAppOptions
                        {
                            public string Name { get; set; } = ""hi mom"";

                            [OutputMask]
                            [OutputMask(PrefixLen=3)]
                            public string Password { get; set; } = ""thisisasecret"";
                        }
                     ";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source, (o) => {
            o.Count().ShouldBe(1);
            o[0].Severity.ShouldBe(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
            o[0].Id.ShouldBe("SEEK002");
        });

    }

    [Fact]
    public Task MissingRegexCauseError()
    {
        // The source code to test
        var source = @"
                        using Seekatar.OptionToStringGenerator;

                        [OptionsToStringAttribute]
                        public class MyAppOptions
                        {
                            public string Name { get; set; } = ""hi mom"";

                            [OutputRegex]
                            public string MissingRegexParam { get; set; } = ""thisisasecret"";

                            [OutputRegex(Regex = ""User Id=([^;]+).*Password=([^;]+"", IgnoreCase = true)]
                            public string BadRegEx { get; set; } = ""hi mom"";

                        }
                     ";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source, (o) => {
            o.Count().ShouldBe(2);
            o[0].Severity.ShouldBe(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
            o[0].Id.ShouldBe("SEEK001");
            o[1].Severity.ShouldBe(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
            o[1].Id.ShouldBe("SEEK001");
        });
    }

    [Fact]
    public Task NoOptions()
    {
        // The source code to test
        var source = File.ReadAllText("TestFiles/NoOptions.cs");

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source, (o) => {
            o.Count().ShouldBe(1);
            o[0].Severity.ShouldBe(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
            o[0].Id.ShouldBe("SEEK003");
        });
    }

    [Fact]
    public Task BadTitle()
    {
        // The source code to test
        var source = @"
                        using Seekatar.OptionToStringGenerator;

                        [OptionsToString(Title=""Hi{Thisdoesntexist}""]
                        public class BadTitleOptions
                        {
                            public string Name { get; set; } = ""hi mom"";
                        }
                     ";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source, (o) => {
            o.Count().ShouldBe(1);
            o[0].Severity.ShouldBe(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
            o[0].Id.ShouldBe("SEEK004");
        });
    }


}