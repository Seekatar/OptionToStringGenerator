#nullable enable
using Seekatar.OptionToStringGenerator;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using System;
using System.Text;
using System.Collections.Generic;

namespace Test;


// ReSharper disable CollectionNeverUpdated.Global
[DebuggerDisplay(nameof(MessagingOptions))]
[OptionsToString]
public class MessagingOptions
{
    public enum StartingOffset
    {
        Earliest,
        Latest
    }

    public const string SectionName = "Messaging";

    public IDictionary<string, ClientOptions>? Consumers { get; set; } = new Dictionary<string, Test.MessagingOptions.ClientOptions>() {
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
            };

    public IDictionary<string, ClientOptions>? Producers { get; set; } = new Dictionary<string, Test.MessagingOptions.ClientOptions>() {
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
            };

    public const string DefaultConfigName = "Default";

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var c in Consumers ?? new Dictionary<string, ClientOptions>())
        {
            sb.AppendLine(c.Value.OptionsToString());
        }
        foreach (var p in Producers ?? new Dictionary<string, ClientOptions>())
        {
            sb.AppendLine(p.Value.OptionsToString());
        }

        return sb.ToString();
    }

    [DebuggerDisplay(nameof(ClientOptions) + " {Name}")]
    [OptionsToString(Title = nameof(ClientOptions) + " {Prefix} {Name}")]
    public class ClientOptions
    {
        private string _ca = "";
        private string _certPem = "";
        private string _keyPem = "";
        private string _groupId = "";

        private static string ExpandNewLines(string s)
        {
            if (s.Contains("-----\\n"))
            {
                return s.Replace("\\n", "\n");
            }
            else
            {
                return s;
            }
        }

        public string Prefix { get; set; } = "";
        public string Name { get; set; } = "";

        [Required]
        [OutputMask(PrefixLen = 3)]
        public string EncryptionKey { get; set; } = "";
        public string GroupId {
            get =>
                string.IsNullOrEmpty(_groupId)
                    ? Assembly.GetEntryAssembly()?.GetName().Name ?? throw new Exception("Can't get Entry Assembly")
                    : _groupId;
            set => _groupId = value;
        }

        public StartingOffset StartingOffset { get; set; }

        public int? MaxPollIntervalMs { get; set; }

        // SSL config
        public string CertBootstrapServers { get; set; } = "";

        [OutputLengthOnly]
        public string CA {
            get => ExpandNewLines(_ca);
            set => _ca = value;
        }

        [OutputLengthOnly]
        public string CertPem {
            get => ExpandNewLines(_certPem);
            set => _certPem = value;
        }

        [OutputLengthOnly]
        public string KeyPem {
            get => ExpandNewLines(_keyPem);
            set => _keyPem = value;
        }

        public string SaslBootstrapServers { get; set; } = "";
        
        [OutputMask(PrefixLen = 3)]
        public string SaslPassword { get; set; } = "";
        public string SaslMechanism { get; set; } = "Plain";
        public string SaslUsername { get; set; } = "$ConnectionString";

        public bool LocalhostBootstrap => CertBootstrapServers.StartsWith("localhost") ||
                                          CertBootstrapServers.StartsWith("127.0.0.1") ||
                                          CertBootstrapServers.StartsWith("host.docker.internal");

        [OutputIgnore]
        public bool ConfiguredAsConsumer => (!string.IsNullOrWhiteSpace(CertBootstrapServers)
                                             || !string.IsNullOrWhiteSpace(SaslBootstrapServers));

        [OutputIgnore]
        public bool ConfiguredAsProducer => !string.IsNullOrWhiteSpace(CertBootstrapServers)
                                            || !string.IsNullOrWhiteSpace(SaslBootstrapServers);

        public bool ConfiguredWithCerts => !string.IsNullOrWhiteSpace(CertBootstrapServers)
                                           && !string.IsNullOrWhiteSpace(CA)
                                           && !string.IsNullOrWhiteSpace(CertPem)
                                           && !string.IsNullOrWhiteSpace(KeyPem);

        public bool ConfiguredWithSasl => !string.IsNullOrWhiteSpace(SaslBootstrapServers)
                                          && !string.IsNullOrWhiteSpace(SaslPassword)
                                          && !string.IsNullOrWhiteSpace(SaslUsername)
                                          && !string.IsNullOrWhiteSpace(SaslMechanism);
    }
}

// ReSharper enable CollectionNeverUpdated.Global
