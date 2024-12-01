using System.ComponentModel.DataAnnotations;
using Seekatar.OptionToStringGenerator;
using System.Collections.Generic;

namespace Test;

[OptionsToString]
public class NullOptions
{
    [OptionsToString]
    public class ArrayItem
    {
    }

    [Required]
    public List<ArrayItem>? ProfilesList { get; set; }

    public IList<ArrayItem>? ProfilesIList { get; set; }
    public IDictionary<string, ArrayItem>? ProfilesIDictionary { get; set; }

    public string? name { get; set; }
    public int? Retries { get; set; }
    public double? RetryDelaySeconds { get; set; }
}
