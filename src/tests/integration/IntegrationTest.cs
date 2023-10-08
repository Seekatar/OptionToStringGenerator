namespace Test;
using Seekatar.OptionToStringGenerator;
using System.Globalization;
using Test;

[UsesVerify]
public class IntegrationTest
{
    const string SnapshotDirectory = "Snapshots";

    public IntegrationTest()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; // for date formatting since different on different OSes
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

    [Fact]
    public Task JsonTest()
    {
        var options = new JsonOptions();
        var s = options.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }

    [Fact]
    public Task TitleTest()
    {
        var options = new TitleOptions();
        var s = options.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }

    [Fact]
    public Task FormatTest()
    {
        var options = new FormattingOptions();
        var s = options.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }
}