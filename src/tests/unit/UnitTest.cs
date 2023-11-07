using Shouldly;

namespace Seekatar.OptionToStringGenerator.Tests;
using static Seekatar.OptionToStringGenerator.DiagnosticTemplates.Ids;

[UsesVerify] // Adds hooks for Verify into XUnit
public class UnitTests
{
    public static IEnumerable<object[]> TestFileData()
    {
        // get all the files in the TestFiles directory
        var files = Directory.GetFiles("TestFiles", "*.cs");
        foreach (var file in files.Where( o => !o.Contains("Negative")))
        {
            yield return new object[] { file };
        }
    }

    public static IEnumerable<object[]> PropertyTestFileData()
    {
        // get all the files in the TestFiles directory
        var files = Directory.GetFiles("PropertyTestFiles", "*.cs");
        foreach (var file in files.Where(o => !o.Contains("Negative")))
        {
            yield return new object[] { file };
        }
    }

    [Theory]
    [MemberData(nameof(TestFileData))]
    public Task HappyPathFiles(string filename)
    {
        // Pass the source code to our helper and snapshot test the output
        return TestHelper.VerifyFile<OptionToStringGenerator>(filename);
    }

    [Theory]
    [MemberData(nameof(PropertyTestFileData))]
    public Task HappyPropertyPathFiles(string filename)
    {
        // Pass the source code to our helper and snapshot test the output
        return TestHelper.VerifyFile<OptionPropertyToStringGenerator>(filename);
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
        return TestHelper.Verify<OptionToStringGenerator>(source, (o) => {
            o.Count().ShouldBe(1);
            o[0].Severity.ShouldBe(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
            o[0].Id.ShouldBe(SEEK002.ToString());
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
        return TestHelper.Verify<OptionToStringGenerator>(source, (o) => {
            o.Count().ShouldBe(2);
            o[0].Severity.ShouldBe(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
            o[0].Id.ShouldBe(SEEK001.ToString());
            o[0].GetMessage().ShouldBe("Bad Regex: You must specify a regex parameter");
            o[1].Severity.ShouldBe(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
            o[1].Id.ShouldBe(SEEK001.ToString());
            o[1].GetMessage().ShouldBe("Bad Regex: Invalid pattern 'User Id=([^;]+).*Password=([^;]+' at offset 32. Not enough )'s.");
        });
    }

    [Fact]
    public Task PrivateClassWarning()
    {
        // The source code to test
        var source = @"
                        using Seekatar.OptionToStringGenerator;

                        [OptionsToStringAttribute]
                        private class MyAppOptions
                        {
                            public string Name { get; set; } = ""hi mom"";
                        }
                     ";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify<OptionToStringGenerator>(source, (o) => {
            o.Count().ShouldBe(1);
            o[0].Severity.ShouldBe(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
            o[0].GetMessage().ShouldBe("The class 'MyAppOptions' is private");
            o[0].Id.ShouldBe(SEEK005.ToString());
        });
    }

    [Fact]
    public Task NegativeNoOptions()
    {
        // The source code to test
        var source = File.ReadAllText("TestFiles/NegativeNoOptions.cs");

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify<OptionToStringGenerator>(source, (o) => {
            o.Count().ShouldBe(1);
            o[0].Severity.ShouldBe(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
            o[0].Id.ShouldBe(SEEK003.ToString());
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
        return TestHelper.Verify<OptionToStringGenerator>(source, (o) => {
            o.Count().ShouldBe(1);
            o[0].Severity.ShouldBe(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
            o[0].GetMessage().ShouldBe("Property 'Thisdoesntexist' not found on BadTitleOptions");
            o[0].Id.ShouldBe(SEEK004.ToString());
        });
    }

    [Fact]
    public Task BadExternalType()
    {
        // The source code to test
        var source = @"
                        using Seekatar.OptionToStringGenerator;

                        public class BadType
                        {
                            [OutputPropertyMask(nameof(ExternalClass.SerialNo))]
                            public Guid Name { get; set; };
                            [OutputPropertyMask(nameof(ExternalClass.SerialNo))]
                            public DateTime Name { get; set; };
                        }
                    ";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify<OptionPropertyToStringGenerator>(source, (o) => {
            o.Count().ShouldBe(2);
            o[0].Severity.ShouldBe(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
            o[0].GetMessage().ShouldBe("The Property 'Name' has invalid type of Guid. Must be class, record, or interface");
            o[0].Id.ShouldBe(SEEK008.ToString());
            o[1].Severity.ShouldBe(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
            o[1].GetMessage().ShouldBe("The Property 'Name' has invalid type of DateTime. Must be class, record, or interface");
            o[1].Id.ShouldBe(SEEK008.ToString());
        });
    }
    [Fact]
    public Task MissingName()
    {
        // The source code to test
        var source = @"
                        using Seekatar.OptionToStringGenerator;

                        public class FakeOptions
                        {
                            public string Name { get; set; };
                        }
                        public class BadType
                        {
                            [OutputPropertyMask("""")]
                            public FakeOptions NoName { get; set; };
                        }
                    ";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify<OptionPropertyToStringGenerator>(source, (o) => {
            o.Count().ShouldBe(1);
            o[0].Severity.ShouldBe(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
            o[0].GetMessage().ShouldBe("The attribute 'OutputPropertyMaskAttribute' didn't have a Name set");
            o[0].Id.ShouldBe(SEEK007.ToString());
        });
    }


}