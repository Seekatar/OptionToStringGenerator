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
        [Required]
        public int MaxTokens { get; set; } = 800;
    }


    [Required]
    [OutputEnumerable]
    public List<ArrayItem> Profiles { get; set; } = new List<ArrayItem>();

    [Required]
    [OutputEnumerable]
    public ArrayItem[] ProfilesArray { get; set; } = Array.Empty<ArrayItem>();

    public int Retries { get; set; } = 3;
    public double RetryDelaySeconds { get; set; } = 3;
}
