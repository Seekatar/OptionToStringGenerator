using System.Text.Json;
using System.Xml.Linq;

namespace Seekatar.OptionToStringGenerator;

/// <summary>
/// Marker attribute to indicate a OptionsToString() extension method should be generated
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class OptionsToStringAttribute : Attribute
{
    public string Indent { get; set; } = "  ";
    public string Separator { get; set; } = ":";
    public bool Json { get; set; } = false;
    public string? Title { get; set; }
    public bool ExcludeParents { get; set; } = false;
}

/// <summary>
/// Marker attribute to should show only prefixLen or suffixLen characters and mask the rest
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class OutputMaskAttribute : Attribute
{
    /// <summary>
    /// Number of unmasked characters at the start of the string
    /// </summary>
    public int PrefixLen { get; set; }
    /// <summary>
    /// Number of unmasked characters at the end of the string
    /// </summary>
    public int SuffixLen { get; set; }
}



/// <summary>
/// Marker attribute to should mask any captures of this regex
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class OutputRegexAttribute : Attribute
{
    /// <summary>
    /// Regex with captures to mask
    /// </summary>
    public string Regex { get; set; } = "";
    /// <summary>
    /// Ignore case when matching
    /// </summary>
    public bool IgnoreCase { get; set; }
}


/// <summary>
/// Marker to only show the length of the string
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class OutputLengthOnlyAttribute : Attribute
{
}

/// <summary>
/// Marker to totally ignore this property
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class OutputIgnoreAttribute : Attribute
{
}

public interface IPropertyAttribute
{
    string Name { get; set; }
}


/// <summary>
/// Marker attribute for formatting the output
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class OutputPropertyFormatAttribute : OptionsToStringAttribute
{
    public OutputPropertyFormatAttribute() { }
}

/// <summary>
/// Marker attribute to should show only prefixLen or suffixLen characters and mask the rest
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class OutputPropertyMaskAttribute : OutputMaskAttribute, IPropertyAttribute
{
    public OutputPropertyMaskAttribute(string name)
    {
        Name = name;
    }
    public string Name { get; set; } = "";
}

/// <summary>
/// Marker attribute to should mask any captures of this regex
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class OutputPropertyRegexAttribute : OutputRegexAttribute, IPropertyAttribute
{
    public OutputPropertyRegexAttribute(string name)
    {
        Name = name;
    }
    public string Name { get; set; } = "";
}

/// <summary>
/// Marker to only show the length of the string
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class OutputPropertyLengthOnlyAttribute : OutputLengthOnlyAttribute, IPropertyAttribute
{
    public OutputPropertyLengthOnlyAttribute(string name)
    {
        Name = name;
    }
    public string Name { get; set; } = "";
}

/// <summary>
/// Marker to totally ignore this property
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class OutputPropertyIgnoreAttribute : OutputIgnoreAttribute, IPropertyAttribute
{
    public OutputPropertyIgnoreAttribute(string name)
    {
        Name = name;
    }
    public string Name { get; set; } = "";
}