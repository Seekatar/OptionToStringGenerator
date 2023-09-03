namespace integration;
using Seekatar.OptionToStringGenerator;

public class MyClass
{
    public string Name { get; set; } = "A name";
    public string Description { get; set; } = "A description";
    public override string ToString() => $"MyClass: {Name}";
}

[OptionsToString]
class MyInternalAppOptions
{
    public string Name { get; set; } = "hi mom";

    public string? NullName { get; set; }

    public MyClass AnObject { get; set; } = new();

    [OutputMask]
    public string Password { get; set; } = "thisisasecret";

    [OutputMask(PrefixLen=3)]
    public string Certificate { get; set; } = "abc1233435667";

    [OutputLengthOnly]
    public string Secret { get; set; } = "thisisasecretthatonlyshowsthelength";

    [OutputRegex(Regex="User Id=([^;]+).*Password=([^;]+)")]
    public string ConnectionString { get; set; } = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";

    [OutputRegex(Regex="User Id=([^;]+).*Password=([^;]+)",IgnoreCase=true)]
    public string ConnectionStringIgnore { get; set; } = "Server=myServerAddress;Database=myDataBase;user Id=myUsername;Password=myPassword;";

    [OutputRegex(Regex = "User Id=([^;]+).*Password=([^;]+)")]
    public string ConnectionStringCase { get; set; } = "Server=myServerAddress;Database=myDataBase;user Id=myUsername;Password=myPassword;";

    [OutputIgnore]
    public string IgnoreMe { get; set; } = "abc1233435667";
}

[OptionsToString]
public class MyPublicAppOptions
{
    public string Name { get; set; } = "hi mom";
}

[OptionsToString]
public class MyPrivateAppOptions
{
    public string Name { get; set; } = "hi mom";
}

[OptionsToString]
class ObjectMasking
{
    public ObjectMasking()
    {
        AnObject = new() { Name = "Server=myServerAddress;Database=myDataBase;user Id=myUsername;Password=myPassword;" };
    }

    [OutputRegex(Regex = "User Id=([^;]+).*Password=([^;]+)", IgnoreCase = true)]
    public MyClass AnObject { get; }
}

[OptionsToString]
class BadOptions
{
    // shows warning about two attributes
    [OutputRegex(Regex = "User Id=([^;]+).*Password=([^;]+)", IgnoreCase = true)]
    [OutputLengthOnly]
    public string Name { get; set; } = "hi mom";

    // will make an error [OutputRegex]
    public string Name2 { get; set; } = "hi mom";

    private string NotShown1 { get; set; } = "bye mom";
    protected string NotShown2 { get; set; } = "bye mom";
    internal string NotShown3 { get; set; } = "bye mom";
    string NotShown4 { get; set; } = "bye mom";
}

[UsesVerify]
public class IntegrationTest
{
    const string SnapshotDirectory = "Snapshots";

    [Fact]
    public Task TestPublicClass()
    {
        var options = new MyPublicAppOptions();
        var s = options.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }

    [Fact]
    public Task TestInternalClass()
    {
        var options = new MyInternalAppOptions();
        var s = options.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }

    [Fact]
    public Task TestPrivateClass()
    {
        var options = new MyPrivateAppOptions();
        var s = options.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }

    [Fact]
    public Task TestObjectMasking()
    {
        var options = new ObjectMasking();
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