namespace integration;
using Seekatar.OptionToStringGenerator;
using System.Globalization;

[OptionsToString]
public class PublicOptions
{
    public class AClass
    {
        public string Name { get; set; } = "maybe this is secret";
        public override string ToString() => $"{nameof(AClass)}: {Name}";
    }

    public string PlainText { get; set; } = "hi mom";

    public int PlainNumber { get; set; } = 42;

    public DateTime PlainDateTime { get; set; } = new DateTime(2020, 1, 2, 3, 4, 5);

    public string? NullItem { get; set; }

    public AClass AnObject { get; set; } = new();

    [OutputRegex(Regex = @"AClass\:\s+(.*)")]
    public AClass AMaskedObject { get; set; } = new();

    [OutputMask]
    public string FullyMasked { get; set; } = "thisisasecret";

    [OutputMask(PrefixLen=3)]
    public string FirstThreeNotMasked { get; set; } = "abc1233435667";

    [OutputMask(PrefixLen = 100)]
    public string NotMaskedSinceLongLength { get; set; } = "abc1233435667";

    [OutputLengthOnly]
    public string LengthOnly { get; set; } = "thisisasecretthatonlyshowsthelength";

    [OutputRegex(Regex="User Id=([^;]+).*Password=([^;]+)")]
    public string MaskUserAndPassword { get; set; } = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";

    [OutputRegex(Regex="User Id=([^;]+).*Password=([^;]+)",IgnoreCase=true)]
    public string MaskUserAndPasswordIngoreCase { get; set; } = "Server=myServerAddress;Database=myDataBase;user Id=myUsername;Password=myPassword;";

    [OutputRegex(Regex = "User Id=([^;]+).*Password=([^;]+)")]
    public string RegexNotMatched { get; set; } = "Server=myServerAddress;Database=myDataBase;user Id=myUsername;Password=myPassword;";

    [OutputIgnore]
    public string IgnoreMe { get; set; } = "abc1233435667";
}

[OptionsToString]
class InternalOptions
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
    public PublicOptions.AClass AnObject { get; }
}

[OptionsToString]
class NoOptions
{
}

[OptionsToString]
class BadOptions
{
    // shows warning about two attributes
    [OutputRegex(Regex = "User Id=([^;]+).*Password=([^;]+)", IgnoreCase = true)]
    [OutputLengthOnly]
    public string Name { get; set; } = "hi mom";

    private string NoteShown1 { get; set; } = "bye mom";
    protected string NoteShown2 { get; set; } = "bye mom";
    internal string NoteShown3 { get; set; } = "bye mom";
    string NoteShown4 { get; set; } = "bye mom";
}

[UsesVerify]
public class IntegrationTest
{
    const string SnapshotDirectory = "Snapshots";

    public IntegrationTest()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; // for date formatting since different on different Oses
    }

    [Fact]
    public Task TestPublicClass()
    {
        var options = new PublicOptions();
        var s = options.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }

    [Fact]
    public Task TestInternalClass()
    {
        var options = new InternalOptions();
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

    [Fact]
    public Task NoOptionTest()
    {
        var options = new NoOptions();
        var s = options.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }
}