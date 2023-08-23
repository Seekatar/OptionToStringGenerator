namespace integration;
using Seekatar.OptionToStringGenerator;
using Seekatar.ClassGenerators; 

[OptionsToString]
public class MyAppOptions
{
    public string Name { get; set; } = "hi mom";

    [OutputMask]
    public string Password { get; set; } = "thisisasecret";

    [OutputMask(PrefixLen=3)]
    public string Certificate { get; set; } = "abc1233435667";

    [OutputLengthOnly]
    public string Secret { get; set; } = "thisisasecretthatonlyshowsthelength";

    [OutputRegex(Regex="User Id=([^;]+).*Password=([^;]+)")]
    public string ConnectionString { get; set; } = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";

    [OutputRegex(Regex="User Id=([^;]+).*Password=([^;]+)",IgnoreCase=true)]
    public string AnotherConnectionString { get; set; } = "Server=myServerAddress;Database=myDataBase;user Id=myUsername;Password=myPassword;";

    [OutputIgnore]
    public string IgnoreMe { get; set; } = "abc1233435667";
}


[UsesVerify]
public class IntegrationTest
{
    [Fact]
    public Task Test()
    {
        var options = new MyAppOptions();
        var s = options.OptionsToString();
        return Verify(s);
    }
}