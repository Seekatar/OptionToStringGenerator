
using Seekatar.OptionToStringGenerator;

namespace Test;

[OptionsToString(Indent = "    ", Separator = "-")]
public class FormattingOptions
{
    public int IntProp { get; set; } = 42;
    public string StringProp { get; set; } = "hi mom";
}

