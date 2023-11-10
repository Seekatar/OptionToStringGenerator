using Seekatar.OptionToStringGenerator;


namespace Test;

internal interface IPropertySimple
{
    public string Secret { get; set; }
}

[OptionsToString]
internal class PropertySimple : IPropertySimple
{
    public string Secret { get; set; } = "";
}

internal class PropertyInterface
{
    [OutputPropertyMask(nameof(IPropertySimple.Secret))]
    public IPropertySimple? PropertySimple { get; set; }
}
