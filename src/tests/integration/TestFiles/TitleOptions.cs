
using Seekatar.OptionToStringGenerator;

namespace Test;

[OptionsToString(Title = nameof(TitleOptions) + "_{StringProp}_{IntProp}")]
public class TitleOptions
{
    public int IntProp { get; set; } = 42;
    public string StringProp { get; set; } = "hi mom";
}

