using System.ComponentModel.DataAnnotations;
using Seekatar.OptionToStringGenerator;
using System.Collections.Generic;

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
        public string DeploymentName { get; set; } = "";
    }

    [Required]
    [OutputDictionary]
    public Dictionary<string, DictionaryItem> StringToProfiles { get; set; } = new()  {
                { "A", new () { ProfileName = "ProfileNameA" } },
                { "B", new() { ProfileName = "ProfileNameB" } }
            };

    [Required]
    [OutputDictionary]
    public Dictionary<int, DictionaryItem> IntToProfiles { get; set; } = new() {
                { 1, new() { ProfileName = "ProfileName1" } },
                { 2, new() { ProfileName = "ProfileName2" } }
            };

    public int Retries { get; set; } = 3;
    public double RetryDelaySeconds { get; set; } = 3;
}
