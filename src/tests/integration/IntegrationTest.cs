namespace Test;
using Seekatar.OptionToStringGenerator;
using System;
using System.Globalization;
using System.Reflection;

[UsesVerify]
public class IntegrationTest
{
    const string SnapshotDirectory = "Snapshots";

    public IntegrationTest()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; // for date formatting since different on different OSes
    }

    public static IEnumerable<object[]> TestFileData()
    {
        yield return new object[] { new InternalOptions() };
        yield return new object[] { new PublicOptions() };
        yield return new object[] { new ObjectMasking() };
        yield return new object[] { new NegativeBadOptions() };
        yield return new object[] { new NegativeNoOptions() };
        yield return new object[] { new JsonOptions() };
        yield return new object[] { new TitleOptions() };
        yield return new object[] { new FormattingOptions() };
        yield return new object[] { new EscapeOptions() };
        yield return new object[] { new MaskingOptions() };
        yield return new object[] { new PropertyTestClass() };
    }

    [Theory]
    [MemberData(nameof(TestFileData))]
    public Task TestClasses(object options)
    {
        var method = typeof(ClassExtensions).GetMethod("OptionsToString", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, new Type[] { options.GetType() } );

        Assert.True(method != null, "Could not find OptionsToString method");
        var s = method.Invoke(options, new object[] { options });
        return Verify(s).UseDirectory(SnapshotDirectory).UseParameters(options.GetType().Name); 
    }

    [Fact]
    public Task ExternalClass()
    {
        var o = new MyExternalClass();
        var s = o.MyExtClassProperty.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }

    [Fact]
    public Task NestedTest()
    {
        var s = Wrapper.GetOptionsString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }

    [Fact]
    public async Task MessagingTest()
    {
        var options = new Test.MessagingOptions() {
            Producers = new Dictionary<string, Test.MessagingOptions.ClientOptions>() {
                { "TestProducer1", new Test.MessagingOptions.ClientOptions() {
                    CA = new string('*', 10),
                    CertBootstrapServers = "certBootstrapServers",
                    CertPem = new string('*', 11),
                    EncryptionKey = new string("1234567889"),
                    Prefix = "Event Hub",
                    Name = "TestName",
                    SaslBootstrapServers = "saslBootstrapServers",
                    SaslMechanism = "saslMechanism",
                    SaslPassword = new string('*', 12),
                } },
                { "TestProducer2", new Test.MessagingOptions.ClientOptions() {
                    CA = new string('*', 10),
                    CertBootstrapServers = "certBootstrapServers2",
                    CertPem = new string('*', 11),
                    EncryptionKey = new string("2222222222222222222222"),
                    Prefix = "Event Hub",
                    Name = "TestName",
                    SaslBootstrapServers = "saslBootstrapServers",
                    SaslMechanism = "saslMechanism",
                    SaslPassword = new string('*', 12),
                } }
            },
            Consumers = new Dictionary<string, Test.MessagingOptions.ClientOptions>() {
                { "TestConsumer", new Test.MessagingOptions.ClientOptions() {
                    CA = new string('*', 10),
                    CertBootstrapServers = "certBootstrapServers",
                    CertPem = new string('*', 11),
                    EncryptionKey = new string("consumer_1test123"),
                    Prefix = "Event Hub",
                    Name = "TestName",
                    SaslBootstrapServers = "saslBootstrapServers",
                    SaslMechanism = "saslMechanism",
                    SaslPassword = new string('*', 12),
                } }
            }
        };
        var s = options.ToString();
        await Verify(s).UseDirectory(SnapshotDirectory);
    }

    [Fact]
    public async Task MessagingClientTest()
    {

        var options = new Test.MessagingOptions.ClientOptions() {
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