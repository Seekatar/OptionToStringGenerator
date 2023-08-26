//using VerifyXunit;

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

                            [OutputMask]
                            public string Password { get; set; } = "thisisasecret";

                            [OutputIgnore]
                            public string IgnoreMe { get; set; } = "abc1233435667";
                     
                            [OutputMask(PrefixLen=3)]
                            public string Certificate { get; set; } = "abc1233435667";

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
        return TestHelper.Verify(source);

    }
}