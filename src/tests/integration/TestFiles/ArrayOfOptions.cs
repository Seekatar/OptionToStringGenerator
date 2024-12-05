#nullable enable
using System.ComponentModel.DataAnnotations;
using Seekatar.OptionToStringGenerator;
using System.Collections.Generic;
using System.Linq;

namespace Test;

[OptionsToString]
public class ArrayOptions
{
    public class NotAnOption
    {
        public string Name { get; set; } = "";
    }

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

    public IList<ArrayItem> ProfilesIList { get => ProfilesList; }
    public IList<ArrayItem>? NullProfilesIList { get; set; }

    public ICollection<ArrayItem>? ArrayItemCollection { get => ProfilesList; }

    [Required]
    public ArrayItem[] ProfilesArray { get => ProfilesIList.ToArray(); }
    public ArrayItem[]? NullProfilesArray { get; set; }

    public ICollection<NotAnOption>? NotAnOptionCollection = new List<NotAnOption>
            {
                new()
                {
                    Name = "Name1"
                },
                new()
                {
                    Name = "Name2"
                }
            };
    public IDictionary<string, NotAnOption>? NotAnOptionDictionary { get; set; } = new Dictionary<string, NotAnOption>
            {
                { "Key1", new NotAnOption { Name = "Name1" } },
                { "Key2", new NotAnOption { Name = "Name2" } }
            };
    public int Retries { get; set; } = 3;
    public double RetryDelaySeconds { get; set; } = 3;
}
