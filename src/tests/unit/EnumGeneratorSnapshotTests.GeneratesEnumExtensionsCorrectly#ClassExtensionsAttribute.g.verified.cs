//HintName: ClassExtensionsAttribute.g.cs
namespace Seekatar.OptionToStringGenerator;

[System.AttributeUsage(System.AttributeTargets.Class)]
public class OptionsToStringAttribute : System.Attribute
{
}
[System.AttributeUsage(System.AttributeTargets.Property)]
public class OptionsToStringFormatAttribute : System.Attribute
{
    public bool Mask { get; set; }
    public bool SqlConnectionString { get; set; }
    public bool LengthOnly { get; set; }
    public int PrefixLen { get; set; }
}
[System.AttributeUsage(System.AttributeTargets.Property)]
public class OptionsToStringIgnoreAttribute : System.Attribute
{
}