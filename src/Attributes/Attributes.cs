namespace Seekatar.OptionToStringGenerator;

[AttributeUsage(AttributeTargets.Class)]
public class OptionsToStringAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property)]
public class OutputMaskAttribute : Attribute
{
    public int PrefixLen { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
public class OutputRegexAttribute : Attribute
{
    public string Regex { get; set; } = "";
    public bool IgnoreCase { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
public class OutputLengthOnlyAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property)]
public class OutputIgnoreAttribute : Attribute
{
}
