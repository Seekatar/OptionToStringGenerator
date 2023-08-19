namespace Seekatar.OptionToStringGenerator;

public static class SourceGenerationHelper
{
    public const string Attribute = @"
namespace Seekatar.OptionToStringGenerator;

[System.AttributeUsage(System.AttributeTargets.Class)]
public class OptionsToStringAttribute : System.Attribute
{
}
[System.AttributeUsage(System.AttributeTargets.Property)]
public class OptionsToStringFormatAttribute : System.Attribute
{
}
";
}