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
    [OutputPropertyMask(Name = nameof(PropertyTestRecord.SerialNo), SuffixLen = 3)]
    [OutputPropertyMask(Name = nameof(PropertyTestRecord.Name))]
    public PropertyTestRecord MyExtClassProperty { get; set; } = new PropertyTestRecord();
}