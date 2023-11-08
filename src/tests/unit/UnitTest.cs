using Microsoft.CodeAnalysis;
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

                            [OutputMask(PrefixLen=3)]
                            [OutputRegex(Regex = ""."")]
                            public string Password { get; set; } = ""thisisasecret"";
                        }
                     ";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify<OptionToStringGenerator>(source, (o) => {
            o.Count().ShouldBe(1);
            o.ShouldSatisfyAllConditions(
                () => o[0].Severity.ShouldBe(DiagnosticSeverity.Warning),
                () => o[0].Id.ShouldBe(SEEK002.ToString()),
                () => o[0].GetMessage().ShouldBe("Multiple format attributes found. Using first one"),
                () => GetLocationText(o[0].Location).ShouldBe("Password")
            );
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
            o.ShouldSatisfyAllConditions(
                () => o[0].Severity.ShouldBe(DiagnosticSeverity.Error),
                () => o[0].Id.ShouldBe(SEEK001.ToString()),
                () => o[0].GetMessage().ShouldBe("Bad Regex: You must specify a regex parameter"),
                () => GetLocationText(o[0].Location).ShouldStartWith("OutputRegex"),
                () => o[1].Severity.ShouldBe(DiagnosticSeverity.Error),
                () => o[1].Id.ShouldBe(SEEK001.ToString()),
                () => o[1].GetMessage().ShouldBe("Bad Regex: Invalid pattern 'User Id=([^;]+).*Password=([^;]+' at offset 32. Not enough )'s."),
                () => GetLocationText(o[1].Location).ShouldStartWith("OutputRegex")
            );
        });
    }

    static string GetLocationText(Location location)
    {
        var tree = location.SourceTree;
        if (tree is null) return string.Empty;
        var text = tree.GetText();
        var span = location.SourceSpan;
        return text.GetSubText(span).ToString();
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
            o.ShouldSatisfyAllConditions(
                () => o[0].Severity.ShouldBe(DiagnosticSeverity.Warning),
                () => o[0].GetMessage().ShouldBe("The class 'MyAppOptions' is private"),
                () => o[0].Id.ShouldBe(SEEK005.ToString()),
                () => GetLocationText(o[0].Location).ShouldStartWith("MyAppOptions")
            );
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
            o.ShouldSatisfyAllConditions(
                () => o[0].Severity.ShouldBe(DiagnosticSeverity.Warning),
                () => o[0].Id.ShouldBe(SEEK003.ToString())
            );
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
            o.ShouldSatisfyAllConditions(
                () => o[0].Severity.ShouldBe(DiagnosticSeverity.Warning),
                () => o[0].GetMessage().ShouldBe("Property 'Thisdoesntexist' not found on BadTitleOptions"),
                () => o[0].Id.ShouldBe(SEEK004.ToString()),
                () => GetLocationText(o[0].Location).ShouldStartWith("OptionsToString")
            );
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
                            public Guid BadGuid { get; set; }
                            [OutputPropertyMask(nameof(ExternalClass.SerialNo))]
                            public DateTime Name { get; set; }
                        }
                    ";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify<OptionPropertyToStringGenerator>(source, (o) => {
            o.Count().ShouldBe(2);
            o.ShouldSatisfyAllConditions(
                    () => o[0].Severity.ShouldBe(DiagnosticSeverity.Error),
                    () => o[0].GetMessage().ShouldBe("The Property 'BadGuid' has invalid type of Guid. Must be class, record, or interface"),
                    () => o[0].Id.ShouldBe(SEEK008.ToString()),
                    () => GetLocationText(o[0].Location).ShouldBe("BadGuid"),
                    () => o[1].Severity.ShouldBe(DiagnosticSeverity.Error),
                    () => o[1].GetMessage().ShouldBe("The Property 'Name' has invalid type of DateTime. Must be class, record, or interface"),
                    () => o[1].Id.ShouldBe(SEEK008.ToString()),
                    () => GetLocationText(o[1].Location).ShouldBe("Name")
                );
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
                            public string Name { get; set; }
                        }
                        public class BadType
                        {
                            [OutputPropertyMask("""")]
                            public FakeOptions NoName { get; set; }
                        }
                    ";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify<OptionPropertyToStringGenerator>(source, (o) => {
            o.Count().ShouldBe(1);
            o.ShouldSatisfyAllConditions(
                () => o[0].Severity.ShouldBe(DiagnosticSeverity.Error),
                () => o[0].GetMessage().ShouldBe("The attribute 'OutputPropertyMaskAttribute' has an empty Name"),
                () => o[0].Id.ShouldBe(SEEK007.ToString()),
                () => GetLocationText(o[0].Location).ShouldStartWith("OutputPropertyMask")
            );
        });
    }

    [Fact]
    public Task BadPropertyName()
    {
        // The source code to test
        var source = @"
                        using Seekatar.OptionToStringGenerator;

                        public class FakeOptions
                        {
                            public string Name { get; set; }
                        }
                        public class BadType
                        {
                            [OutputPropertyMask(""BadName"")]
                            public FakeOptions NoName { get; set; }
                        }
                    ";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify<OptionPropertyToStringGenerator>(source, (o) => {
            o.Count().ShouldBe(1);
            o.ShouldSatisfyAllConditions(
                () => o[0].Severity.ShouldBe(DiagnosticSeverity.Error),
                () => o[0].GetMessage().ShouldBe("The member 'BadName' in the attribute isn't in the class 'FakeOptions'"),
                () => o[0].Id.ShouldBe(SEEK006.ToString()),
                () => GetLocationText(o[0].Location).ShouldStartWith("OutputPropertyMask")
            );
        });
    }



}