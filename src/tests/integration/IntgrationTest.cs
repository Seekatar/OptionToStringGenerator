namespace integration;
using Seekatar.OptionToStringGenerator;

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


[OptionsToString]
public class BadOptions
{
    // shows warning about two attributes
    [OutputRegex(Regex = "User Id=([^;]+).*Password=([^;]+)", IgnoreCase = true)]
    [OutputLengthOnly]
    public string Name { get; set; } = "hi mom";

    // will make an error [OutputRegex]
    public string Name2 { get; set; } = "hi mom";
}

[UsesVerify]
public class IntegrationTest
{
    const string SnapshotDirectory = "Snapshots";

    [Fact]
    public Task Test()
    {
        var options = new MyAppOptions();
        var s = options.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }

    [Fact]
    public Task BadOptionTest()
    {
        var options = new BadOptions();
        var s = options.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }
}