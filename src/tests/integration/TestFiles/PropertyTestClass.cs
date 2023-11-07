using Seekatar.OptionToStringGenerator;

namespace Test;

// mimics the record below just to make sure OutputPropertyMaskAttribute generates the same code as this 
[OptionsToString]
internal class PropertyTestClass
{
    public int Number { get; set; } = 999;
    [OutputMask]
    public string Name { get; set; } = "hi mom";
    [OutputMask(SuffixLen = 3)]
    public string SerialNo { get; set; } = "1234567890";
}
