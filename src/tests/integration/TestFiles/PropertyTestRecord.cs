using Seekatar.OptionToStringGenerator;

namespace Test;

internal record PropertyTestRecord
{
    public int Number { get; set; } = 999;
    public string Name { get; set; } = "hi mom";
    public string SerialNo { get; set; } = "1234567890";
}

internal class MyExternalClass
{
    [OutputPropertyMask(nameof(PropertyTestRecord.SerialNo), SuffixLen = 3)]
    [OutputPropertyMask(nameof(PropertyTestRecord.Name))]
    public PropertyTestRecord MyExtClassProperty { get; set; } = new PropertyTestRecord();
}