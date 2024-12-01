using Seekatar;

namespace Test;

using Seekatar.OptionToStringGenerator;
using System;
using System.Globalization;
using System.Reflection;
using Test.Next.Level;

[UsesVerify]
public class IntegrationTest
{
    const string SnapshotDirectory = "Snapshots";

    public IntegrationTest()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; // for date formatting since different on different OSes
    }

    public static IEnumerable<object[]> TestObjects()
    {
        yield return new object[] { new InternalOptions() };
        yield return new object[] { new PublicOptions() };
        yield return new object[] { new PublicOptionsSorted() };
        yield return new object[] { new ObjectMasking() };
        yield return new object[] { new NegativeBadOptions() };
        yield return new object[] { new NegativeNoOptions() };
        yield return new object[] { new TitleOptions() };
        yield return new object[] { new FormattingOptions() };
        yield return new object[] { new EscapeOptions() };
        yield return new object[] { new MaskingOptions() };
        yield return new object[] { new ChildOptions() };
        yield return new object[] { new ChildOnlyOptions() };
        yield return new object[] { new NamespaceTest() };
        yield return new object[] { new FormatOptions() };
        yield return new object[] { new FormatOptionsJson() };
        yield return new object[] { new OuterOptions() };
        yield return new object[] { new ProviderOptions() };
        yield return new object[] { new Parent() };
        yield return new object[] { new ParentOfNested() };
        yield return new object[] { new ArrayOptions() };
        yield return new object[] { new DictionaryOptions() };
        yield return new object[] { new MessagingOptions() };
        yield return new object[] { new ArrayAndDictionaryOfOptions() };

        // property tests
        yield return new object[] { new PropertyTestClass() };
        yield return new object[] { new PropertySimple() };
        yield return new object[] { new PropertyNamespaceTestRecord() };

    }

    [OptionsToString]
    public record recordTest { }

    internal static string OptionsToString(Test.ProviderOptions o)
    {
        return $@"Test.ProviderOptions:
  DoubleProp             : {Seekatar.Mask.Format(o?.DoubleProp, formatMethod: (o) => Test.ProviderOptions.MyDoubleNoQuotes(o), noQuote: true)}
  DoublePropNullable     : {Seekatar.Mask.Format(o?.DoublePropNullable, formatMethod: (o) => Test.ProviderOptions.MyDoubleNoQuotes(o), noQuote: true)}
  DoublePropNullableNull : {Seekatar.Mask.Format(o?.DoublePropNullableNull, formatMethod: (o) => Test.ProviderOptions.MyDoubleNoQuotes(o), noQuote: true)}
  StringPropNullable     : {Seekatar.Mask.Format(o?.StringPropNullable, formatMethod: (o) => Test.ProviderOptions.MyStringQuotes(o), noQuote: false)}
  StringPropNullableNull : {Seekatar.Mask.Format(o?.StringPropNullableNull, formatMethod: (o) => Test.ProviderOptions.MyStringQuotes(o), noQuote: true)}
";
    }

    [Fact]
    public Task OneOffForDebugging()
    {

        var ss = new recordTest();
        // records don't work???
        // ss.OptionsToString();

        var o = new MessagingOptions();
        var s = o.OptionsToString(extraIndent:"  ");
        return Verify(s).UseDirectory(SnapshotDirectory).UseParameters(o.GetType().Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("NULLL")]
    [InlineData("not set")]
    public Task NullLiteralTest(string? nullLiteral)
    {
        Mask.NullLiteral = nullLiteral;
        var o = new NullOptions();
        var s = o.OptionsToString();
        Mask.NullLiteral = null;
        return Verify(s).UseDirectory(SnapshotDirectory).UseParameters(nullLiteral);
    }

    [Fact]
    public Task ArrayOfOptionsWithExtraIndent()
    {
        var o = new ArrayOptions();
        var s = o.OptionsToString(extraIndent:"  ");
        return Verify(s).UseDirectory(SnapshotDirectory).UseParameters(o.GetType().Name);
    }

    [Fact]
    public Task DictionaryOfOptionsWithExtraIndent()
    {
        var o = new DictionaryOptions();
        var s = o.OptionsToString(extraIndent:"  ");
        return Verify(s).UseDirectory(SnapshotDirectory).UseParameters(o.GetType().Name);
    }

    [Theory]
    [MemberData(nameof(TestObjects))]
    public Task TestClasses(object options)
    {
        var method = typeof(ClassExtensions).GetMethod("OptionsToString", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, new Type[] { options.GetType(), typeof(string), typeof(string) });

        Assert.True(method != null, $"IntegrationTests.cs could not find OptionsToString method on {options.GetType().Name}");
        var s = method.Invoke(options, new object[] { options, "", "" });
        return Verify(s).UseDirectory(SnapshotDirectory).UseParameters(options.GetType().Name);
    }

    [Fact]
    public async Task ValidateJson()
    {
        var o = new JsonOptions();
        var s = o.OptionsToString();
        System.Text.Json.JsonSerializer.Deserialize<JsonOptions>(s);
        await Verify(s).UseDirectory(SnapshotDirectory);
    }

    [Fact]
    public Task ExternalClass()
    {
        var o = new PropertyTestSimple();
        var s = o.MyExtClassProperty.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }

    [Fact]
    public async Task ExternalClassInheritance()
    {
        var o = new PropertyInheritance();
        var s = o.ChildOptions.OptionsToString();
        s += Environment.NewLine;
        s += o.ChildOnlyOptions.OptionsToString();
        await Verify(s).UseDirectory(SnapshotDirectory);
    }

    [Fact]
    public Task PropertyPublicTest()
    {
        var o = new PropertyPublicTest();
        if (o.PublicOptions == null)
        {
            throw new Exception("PublicOptions is null");
        }
        var s = o.PublicOptions.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }

    [Fact]
    public Task PropertyPublicTestSorted()
    {
        var o = new PropertyPublicTestSorted();
        if (o.PublicOptionsSorted == null)
        {
            throw new Exception("PublicOptions is null");
        }
        var s = o.PublicOptionsSorted.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }

    [Fact]
    public Task PropertyNullInterfaceTest()
    {
        var o = new PropertyInterface();
        var s = o.PropertySimple!.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }

    [Fact]
    public Task PropertyInterfaceTest()
    {
        var o = new PropertyInterface() { PropertySimple = new PropertySimple() };
        var s = o.PropertySimple!.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }

    [Fact]
    public Task NestedTest()
    {
        var s = Wrapper.GetOptionsString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }

    //[Fact]
    //public async Task MessagingTest()
    //{
    //    var options = new MessagingOptions();
    //    var s = options.ToString();
    //    await Verify(s).UseDirectory(SnapshotDirectory);
    //}

    [Fact]
    public async Task MessagingClientTest()
    {

        var options = new MessagingOptions.ClientOptions() {
            CA = new string('*', 10),
            CertBootstrapServers = "certBootstrapServers",
            CertPem = new string('*', 11),
            EncryptionKey = new string("asdfasdfsadfsadf*asddsafsd"),
            Prefix = "Event Hub",
            Name = "TestName",
            SaslBootstrapServers = "saslBootstrapServers",
            SaslMechanism = "saslMechanism",
            SaslPassword = new string('*', 12),
        };
        var s = options.OptionsToString();
        await Verify(s).UseDirectory(SnapshotDirectory);
    }
}