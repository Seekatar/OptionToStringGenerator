using System.ComponentModel.DataAnnotations;
using Seekatar.OptionToStringGenerator;

namespace Test;

[OptionsToString]
public class ArrayOptions
{
    [OptionsToString]
    public class ArrayItem
    {
        [Required]
        public string ProfileName { get; set; } = "";
        [Required]
        [OutputMask(PrefixLen = 2, SuffixLen = 2)]
        public string OpenApiKey { get; set; } = "";
        [Required]
        public string DeploymentName { get; set; } = "";
        [Required]
        public string Endpoint { get; set; } = "";
        public string? OpenAiVersion { get; set; }
        public bool OpenAiEnableLogging { get; set; }
        [Required]
        [OutputRegex(Regex = ".{1,80}(.*)")]
        public string SystemMessage { get; set; } = "";
        public int MaxTokens { get; set; } = 800;
        public float Temperature { get; set; } = 0.5f;
        public float NucleusSamplingFactor { get; set; } = 0.95f;
        public float FrequencyPenalty { get; set; } = 0;
        public float PresencePenalty { get; set; } = 0;
        public bool DumpJson { get; set; }
        public bool JsonMode { get; set; }
        public string? TimingCsvFile { get; set; }
    }

    [Required]
    public IList<ArrayItem> Profiles { get; set; } = new List<ArrayItem>();

    public int Retries { get; set; } = 3;
    public double RetryDelaySeconds { get; set; } = 3;
}
