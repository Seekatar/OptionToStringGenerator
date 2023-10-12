
using Seekatar.OptionToStringGenerator;

namespace Test;

[OptionsToString]
public class EscapeOptions
{
    [OutputRegex(Regex = @".*{(\w*)}.*")]
    public string Name { get; set; } = ">> {secret} <<";
}

