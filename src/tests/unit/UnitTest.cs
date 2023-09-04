using Shouldly;

namespace Seekatar.OptionToStringGenerator.Tests;

[UsesVerify] // Adds hooks for Verify into XUnit
public class UnitTests
{
    [Fact]
    public Task GeneratesOptionStringExtensionCorrectly()
    {
        // The source code to test
        var source = """
                        using Seekatar.OptionToStringGenerator;

                        [OptionsToString]
                        public class MyAppOptions
                        {
                            public string Name { get; set; } = "hi mom";

                            public string? NullName { get; set; };

                            [OutputMask]
                            public string Password { get; set; } = "thisisasecret";

                            [OutputIgnore]
                            public string IgnoreMe { get; set; } = "abc1233435667";
                     
                            [OutputMask(PrefixLen=3)]
                            public string Certificate { get; set; } = "abc1233435667";

                            [OutputMask(PrefixLen=30)]
                            public string CertificateShort { get; set; } = "abc1233435667";

                            [OutputLengthOnly]
                            public string Secret { get; set; } = "thisisasecretthatonlyshowsthelength";

                            [OutputRegex(Regex="User Id=([^;]+).*Password=([^;]+)")]
                            public string ConnectionString { get; set; } = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";
                     
                            [OutputRegex(Regex="User Id=([^;]+).*Password=([^;]+)",IgnoreCase=true)]
                            public string AnotherConnectionString { get; set; } = "Server=myServerAddress;Database=myDataBase;user Id=myUsername;Password=myPassword;";
                     
                        }
                     """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesOptionStringExtensionCorrectlyForInternalClass()
    {
        // The source code to test
        var source = """
                        using Seekatar.OptionToStringGenerator;

                        [OptionsToString]
                        class MyAppOptions
                        {
                            public string Name { get; set; } = "hi mom";
                        }
                     """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task MultipleAttributesCauseWarning()
    {
        // The source code to test
        var source = """
                        using Seekatar.OptionToStringGenerator;

                        [OptionsToStringAttribute]
                        public class MyAppOptions
                        {
                            public string Name { get; set; } = "hi mom";

                            [OutputMask]
                            [OutputMask(PrefixLen=3)]
                            public string Password { get; set; } = "thisisasecret";
                        }
                     """;

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
        var source = """
                        using Seekatar.OptionToStringGenerator;

                        [OptionsToStringAttribute]
                        public class MyAppOptions
                        {
                            public string Name { get; set; } = "hi mom";

                            [OutputRegex]
                            public string MissingRegexParam { get; set; } = "thisisasecret";

                            [OutputRegex(Regex = "User Id=([^;]+).*Password=([^;]+", IgnoreCase = true)]
                            public string BadRegEx { get; set; } = "hi mom";

                        }
                     """;

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
        var source = """
                        using Seekatar.OptionToStringGenerator;

                        [OptionsToString]
                        class NoOptions
                        {
                        }
                     """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source, (o) => {
            o.Count().ShouldBe(1);
            o[0].Severity.ShouldBe(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
            o[0].Id.ShouldBe("SEEK003");
        });
    }


}