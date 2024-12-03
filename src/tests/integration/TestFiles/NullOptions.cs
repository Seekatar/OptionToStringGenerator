using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Seekatar.OptionToStringGenerator;

namespace Test;

[OptionsToString]
public class NullOptions
{
    [OptionsToString]
    public class ArrayItem
    {
        public int AvoidWarning { get; set; }
    }

    [Required]
    public List<ArrayItem>? ProfilesList { get; set; }

    public IList<ArrayItem>? ProfilesIList { get; set; }
    public IDictionary<string, ArrayItem>? ProfilesIDictionary { get; set; }

    public string? Name { get; set; }
    public int? Retries { get; set; }
    public double? RetryDelaySeconds { get; set; }
}
