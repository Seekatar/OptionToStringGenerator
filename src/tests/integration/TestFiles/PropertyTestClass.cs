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


internal record PropertyTestRecord
{
    public int Number { get; set; } = 999;
    public string Name { get; set; } = "hi mom";
    public string SerialNo { get; set; } = "1234567890";
}

internal class MyExternalClass
{
    [OutputPropertyMask(Name = nameof(PropertyTestClass.SerialNo), SuffixLen = 3)]
    [OutputPropertyMask(Name = nameof(PropertyTestClass.Name))]
    public PropertyTestRecord MyExtClassProperty { get; set; } = new PropertyTestRecord();
}