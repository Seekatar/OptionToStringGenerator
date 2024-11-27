using System.ComponentModel.DataAnnotations;
using Seekatar.OptionToStringGenerator;
using System.Collections.Generic;

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
        public string DeploymentName { get; set; } = "";
        [Required]
        public string Endpoint { get; set; } = "";
        [Required]
        public int MaxTokens { get; set; } = 800;
    }

    [Required]
    public List<ArrayItem> ProfilesList { get; set; } = new List<ArrayItem>
            {
                new()
                {
                    ProfileName = "ProfileName1"
                },
                new()
                {
                    ProfileName = "ProfileName2"
                }
            };

    public IList<ArrayItem> ProfilesIList { get; set; } = new List<ArrayItem>
            {
                new()
                {
                    ProfileName = "ProfileName1"
                },
                new()
                {
                    ProfileName = "ProfileName2"
                }
            };
    public IList<ArrayItem>? NullProfilesIList { get; set; }

    [Required]
    public ArrayItem[] ProfilesArray { get; set; } = new ArrayItem[]
            {
                new()
                {
                    ProfileName = "ProfileName1"
                },
                new()
                {
                    ProfileName = "ProfileName2"
                }
            };
    public ArrayItem[]? NullProfilesArray { get; set; }

    public int Retries { get; set; } = 3;
    public double RetryDelaySeconds { get; set; } = 3;
}
