using System.ComponentModel.DataAnnotations;
using Seekatar.OptionToStringGenerator;

namespace Test;

[OptionsToString]
public class DictionaryOptions
{
    [OptionsToString]
    public class DictionaryItem
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
        [Required]
        public int MaxTokens { get; set; } = 800;
    }


    [Required]
    [OutputDictionary]
    public Dictionary<string, DictionaryItem> StringToProfiles { get; set; } = new();

    [Required]
    [OutputDictionary]
    public Dictionary<int, DictionaryItem> IntToProfiles { get; set; } = new();

    public int Retries { get; set; } = 3;
    public double RetryDelaySeconds { get; set; } = 3;
}
