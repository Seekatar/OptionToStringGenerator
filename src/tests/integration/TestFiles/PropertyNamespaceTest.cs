namespace Test.Next.Level;

using Seekatar.OptionToStringGenerator;

internal record PropertyNamespaceTestRecord
{
    public int NumberA { get; set; } = 999;
    public string NameA { get; set; } = "hi mom";
    public string SerialNum { get; set; } = "1234567890";
}

internal class PropertyNamespaceTestSimple
{
    [OutputPropertyFormat(Indent = ">   ", Separator = "-", Title = "Custom Title {NameA}")]
    [OutputPropertyMask(nameof(PropertyNamespaceTestRecord.SerialNum), SuffixLen = 3)]
    [OutputPropertyMask(nameof(PropertyNamespaceTestRecord.NameA))]
    public PropertyNamespaceTestRecord MyExtClassProperty { get; set; } = new PropertyNamespaceTestRecord();
}
