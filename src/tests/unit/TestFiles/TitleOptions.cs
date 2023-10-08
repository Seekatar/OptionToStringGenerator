
using Seekatar.OptionToStringGenerator;

namespace Test;

[OptionsToString(Title= "TitleOptions_{StringProp}_{IntProp}")]
public class TitleOptions
{
    public int IntProp { get; set; } = 42;
    public string StringProp { get; set; } = "hi mom";
}

