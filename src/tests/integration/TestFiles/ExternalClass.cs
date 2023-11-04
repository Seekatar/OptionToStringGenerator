using Seekatar.OptionToStringGenerator;

namespace Test;

[OptionsToString]
internal class ExternalClass
{
    public int Number { get; set; } = 999;
    [OutputMask]
    public string Name { get; set; } = "hi mom";
    [OutputMask(SuffixLen = 3)]
    public string SerialNo { get; set; } = "1234567890";
}

[OptionsToString]
record MyRecord
{
    public int Number { get; set; } = 999;
    public string Name { get; set; } = "hi mom";
    public string SerialNo { get; set; } = "1234567890";
}

internal class MyExternalClass
{
    [OutputPropertyMask(Name = nameof(ExternalClass.SerialNo), SuffixLen = 3)]
    [OutputPropertyMask(Name = nameof(ExternalClass.Name))]
    public MyRecord MyExtClassProperty { get; set; } = new MyRecord();

    //[OutputPropertyMask(Name = nameof(ExternalClass.SerialNo))]
    //[OutputPropertyMask(Name = nameof(ExternalClass.Name))]
    //public int MyInt { get; set; }

    //[OutputPropertyMask(Name = nameof(ExternalClass.SerialNo))]
    //[OutputPropertyMask(Name = nameof(ExternalClass.Name))]
    //public MyRecord MyRec { get; set; }

    //[OutputPropertyMask(Name = nameof(ExternalClass.SerialNo))]
    //[OutputPropertyMask(Name = nameof(ExternalClass.Name))]
    //public IPropertyAttribute MyInterface { get; set; }
}