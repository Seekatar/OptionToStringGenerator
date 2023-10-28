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
        var options = new NegativeBadOptions();
        var s = options.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }

    [Fact]
    public Task NoOptionTest()
    {
        var options = new NegativeNoOptions();
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
    public Task EscapeTest()
    {
        var options = new EscapeOptions();
        var s = options.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
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

    [Fact]
    public Task MaskTest()
    {
        var options = new MaskingOptions();
        var s = options.OptionsToString();
        return Verify(s).UseDirectory(SnapshotDirectory);
    }
}