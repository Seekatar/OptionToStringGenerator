#nullable enable
using Microsoft.CodeAnalysis;
using Shouldly;
using System;
using System.Collections.Generic;

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
    public Task OneOffTestForDebugging()
    {
        // return TestHelper.VerifyFile<OptionToStringGenerator>("TestFiles/ArrayOfOptions.cs");

        return TestHelper.VerifyFile<OptionToStringGenerator>("TestFiles/TripleNestedOptions.cs");
    }

    [Fact]
    public Task OneOffTestForDebuggingProperty()
    {
        return TestHelper.VerifyFile<OptionPropertyToStringGenerator>("PropertyTestFiles/PropertyInheritance.cs");
    }

    [Fact]
    public Task MultipleAttributesCauseWarning()
    {
        // The source code to test
        var source = @"
                        using Seekatar.OptionToStringGenerator;

                        [OptionsToString]
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

                        [OptionsToString]
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

                        [OptionsToString]
                        private class MyAppOptions
                        {
                            public string Name { get; set; } = ""hi mom"";
                        }
                     ";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify<OptionToStringGenerator>(source, (o) => {
            o.Count().ShouldBe(1);
            o.ShouldSatisfyAllConditions(
                () => o[0].Severity.ShouldBe(DiagnosticSeverity.Error),
                () => o[0].GetMessage().ShouldBe("Elements defined in a namespace cannot be explicitly declared as private, protected, protected internal, or private protected"),
                () => o[0].Id.ShouldBe("CS1527"),
                () => GetLocationText(o[0].Location).ShouldStartWith("MyAppOptions")
            );
        }, throwCompilerErrors:false);
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

                        [OptionsToString(Title=""Hi{Thisdoesntexist}"")]
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
                        using System;

                        public class BadType
                        {
                            [OutputPropertyMask(nameof(string.Length))]
                            public Guid BadGuid { get; set; }
                            [OutputPropertyMask(nameof(string.Length))]
                            public DateTime Name { get; set; }
                        }
                    ";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify<OptionPropertyToStringGenerator>(source, (o) => {
            o.Count().ShouldBe(2);
            o.ShouldSatisfyAllConditions(
                    () => o[0].Severity.ShouldBe(DiagnosticSeverity.Error),
                    () => o[0].GetMessage().ShouldBe("The Property 'BadGuid' of type 'Guid' is type 'Struct'. It must be class, record, or interface"),
                    () => o[0].Id.ShouldBe(SEEK008.ToString()),
                    () => GetLocationText(o[0].Location).ShouldBe("BadGuid"),
                    () => o[1].Severity.ShouldBe(DiagnosticSeverity.Error),
                    () => o[1].GetMessage().ShouldBe("The Property 'Name' of type 'DateTime' is type 'Struct'. It must be class, record, or interface"),
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

    [Fact]
    public Task BadMethodProvider()
    {
        // The source code to test
        var source = @"
                        using Seekatar.OptionToStringGenerator;

                        [OptionsToString]
                        public class BadProvider
                        {
                            [OutputFormatProvider(typeof(BadProvider), ""badName"")]
                            public string Something{ get; set; }
                        }
                    ";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify<OptionToStringGenerator>(source, (o) => {
            o.Count().ShouldBe(1);
            o.ShouldSatisfyAllConditions(
                () => o[0].Severity.ShouldBe(DiagnosticSeverity.Error),
                () => o[0].GetMessage().ShouldBe("Could not find the method 'badName' on type BadProvider"),
                () => o[0].Id.ShouldBe(SEEK009.ToString()),
                () => GetLocationText(o[0].Location).ShouldStartWith("Something")
            );
        });
    }

    [Fact]
    public Task BadMethodProviderSignature()
    {
        // The source code to test
        var source = @"
                        using Seekatar.OptionToStringGenerator;

                        [OptionsToString]
                        public class BadProvider
                        {

                            public static string ProviderNoParam() => ""badName"";
                            public static void ProviderBadReturn(string _) { return; }
                            private static string PrivateGood(string x) => ""x"";
                            public string NotStatic(string x) => x;
                            public static int NotStringReturn(string x) => 1;
                            public static string RefParam(ref string x) => x;
                            public static string NotStringParam(int x) => """";

                            [OutputFormatProvider(typeof(BadProvider), nameof(ProviderNoParam))]
                            public string SomethingA { get; set; }

                            [OutputFormatProvider(typeof(BadProvider), nameof(ProviderBadReturn))]
                            public string SomethingB { get; set; }

                            [OutputFormatProvider(typeof(BadProvider), nameof(PrivateGood))]
                            public string SomethingC { get; set; }

                            [OutputFormatProvider(typeof(BadProvider), nameof(NotStatic))]
                            public string SomethingD { get; set; }

                            [OutputFormatProvider(typeof(BadProvider), nameof(NotStringReturn))]
                            public string SomethingE { get; set; }

                            [OutputFormatProvider(typeof(BadProvider), nameof(RefParam))]
                            public string SomethingF { get; set; }

                            [OutputFormatProvider(typeof(BadProvider), nameof(NotStringParam))]
                            public string SomethingG { get; set; }
                        }
                    ";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify<OptionToStringGenerator>(source, (o) => {
            o.Count().ShouldBe(7);
            o.ShouldSatisfyAllConditions(
                () => o[0].Severity.ShouldBe(DiagnosticSeverity.Error),
                () => o[0].GetMessage().ShouldBe("The signature of 'BadProvider.ProviderNoParam' should be static string? ProviderNoParam(string)"),
                () => o[0].Id.ShouldBe(SEEK010.ToString()),
                () => GetLocationText(o[0].Location).ShouldStartWith("SomethingA"),

                () => o[1].Severity.ShouldBe(DiagnosticSeverity.Error),
                () => o[1].GetMessage().ShouldBe("The signature of 'BadProvider.ProviderBadReturn' should be static string? ProviderBadReturn(string)"),
                () => o[1].Id.ShouldBe(SEEK010.ToString()),
                () => GetLocationText(o[1].Location).ShouldStartWith("SomethingB"),

                () => o[2].Severity.ShouldBe(DiagnosticSeverity.Error),
                () => o[2].GetMessage().ShouldBe("The signature of 'BadProvider.PrivateGood' should be static string? PrivateGood(string)"),
                () => o[2].Id.ShouldBe(SEEK010.ToString()),
                () => GetLocationText(o[2].Location).ShouldStartWith("SomethingC"),

                () => o[3].Severity.ShouldBe(DiagnosticSeverity.Error),
                () => o[3].GetMessage().ShouldBe("The signature of 'BadProvider.NotStatic' should be static string? NotStatic(string)"),
                () => o[3].Id.ShouldBe(SEEK010.ToString()),
                () => GetLocationText(o[3].Location).ShouldStartWith("SomethingD"),

                () => o[4].Severity.ShouldBe(DiagnosticSeverity.Error),
                () => o[4].GetMessage().ShouldBe("The signature of 'BadProvider.NotStringReturn' should be static string? NotStringReturn(string)"),
                () => o[4].Id.ShouldBe(SEEK010.ToString()),
                () => GetLocationText(o[4].Location).ShouldStartWith("SomethingE"),

                () => o[5].Severity.ShouldBe(DiagnosticSeverity.Error),
                () => o[5].GetMessage().ShouldBe("The signature of 'BadProvider.RefParam' should be static string? RefParam(string)"),
                () => o[5].Id.ShouldBe(SEEK010.ToString()),
                () => GetLocationText(o[5].Location).ShouldStartWith("SomethingF"),

                () => o[6].Severity.ShouldBe(DiagnosticSeverity.Error),
                () => o[6].GetMessage().ShouldBe("The signature of 'BadProvider.NotStringParam' should be static string? NotStringParam(string)"),
                () => o[6].Id.ShouldBe(SEEK010.ToString()),
                () => GetLocationText(o[6].Location).ShouldStartWith("SomethingG")
            );
        });
    }



}