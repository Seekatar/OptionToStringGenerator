using Seekatar.OptionToStringGenerator;


namespace Test;

internal interface IPropertySimple
{
    public string Secret { get; set; }
}

[OptionsToString]
internal class PropertySimple : IPropertySimple
{
    [OutputMask]
    public string Secret { get; set; } = "Secret";
}

internal class PropertyInterface
{
    [OutputPropertyMask(nameof(IPropertySimple.Secret))]
    public IPropertySimple? PropertySimple { get; set; }
}
