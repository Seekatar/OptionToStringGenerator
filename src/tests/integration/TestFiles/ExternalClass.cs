using Seekatar.OptionToStringGenerator;
using System.Reflection;

namespace Test;

internal class ExternalClass
{
    public int Number { get; set; } = 999;
    public string Name { get; set; } = "hi mom";
    public string SerialNo { get; set; } = "1234567890";
}

record MyRecord
{
    public int Number { get; set; } = 999;
    public string Name { get; set; } = "hi mom";
    public string SerialNo { get; set; } = "1234567890";
}

class MyExternalClassClass
{
    [OutputPropertyMask(Name = nameof(ExternalClass.SerialNo))]
    [OutputPropertyMask(Name = nameof(ExternalClass.Name))]
    public ExternalClass MyExtClassProperty { get; set; }

    [OutputPropertyMask(Name = nameof(ExternalClass.SerialNo))]
    [OutputPropertyMask(Name = nameof(ExternalClass.Name))]
    public int MyInt { get; set; }

    [OutputPropertyMask(Name = nameof(ExternalClass.SerialNo))]
    [OutputPropertyMask(Name = nameof(ExternalClass.Name))]
    public MyRecord MyRec { get; set; }

    [OutputPropertyMask(Name = nameof(ExternalClass.SerialNo))]
    [OutputPropertyMask(Name = nameof(ExternalClass.Name))]
    public IPropertyAttribute MyInterface { get; set; }
}