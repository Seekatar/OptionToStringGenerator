using System.Text.Json;
using System.Xml.Linq;

namespace Seekatar.OptionToStringGenerator;

/// <summary>
/// Marker attribute to indicate a OptionsToString() extension method should be generated
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class OptionsToStringAttribute : Attribute
{
    public bool ExcludeParents { get; set; } = false;
    public bool Json { get; set; } = false;
    public bool Sort { get; set; } = false;
    public const string NullLiteral = "null";
    public string Indent { get; set; } = "  ";
    public string Separator { get; set; } = ":";
    public string? Title { get; set; }

    /// <summary>
    /// Helper for formatting objects for output, called by generated code
    /// </summary>
    /// <param name="o">object to do ToString() on</param>
    /// <param name="lengthOnly">only show length</param>
    /// <param name="prefixLen">mask all but prefix</param>
    /// <param name="regex">Regex to mask</param>
    /// <param name="ignoreCase">ignore case on regex</param>
    /// <param name="asJson">for lengthOnly, render as JSON</param>
    /// <returns></returns>
    [Obsolete("Use Mask.Format or Mask.Mask* instead")]
    public static string Format(object? o, bool lengthOnly = false, int prefixLen = -1, int suffixLen = -1, string? regex = null, bool ignoreCase = false, bool asJson = false)
    {
        return Mask.Format(o, lengthOnly, prefixLen, suffixLen, regex, ignoreCase, asJson) ?? (o?.ToString() ?? NullLiteral);
    }
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

/// <summary>
/// Marker attribute to supply a format string to ToString()
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class OutputFormatToStringAttribute : Attribute
{
    /// <summary>
    /// Initialize with a format string pass ToString(), which depends on the type
    /// </summary>
    public OutputFormatToStringAttribute(string format)
    {
        Format = format;
    }

    /// <summary>
    /// Format string pass ToString(), which depends on the type
    /// </summary>
    public string Format { get; set; }
}

/// <summary>
/// Marker attribute for alternate formatting method
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class OutputFormatProviderAttribute : Attribute
{
    /// <summary>
    /// Initialize with a type and method to format the item
    /// </summary>
    /// <param name="formatType"></param>
    /// <param name="method"></param>
    /// <param name="noQuote">Do not quote the result</param>
    public OutputFormatProviderAttribute(Type formatType, string method, bool noQuote = false)
    {
        FormatType = formatType;
        FormatMethod = method;
        NoQuote = noQuote;
    }

    /// <summary>
    /// Class that has the method to format the item
    /// </summary>
    public Type FormatType { get; }

    /// <summary>
    /// Method to format the item, which must be static, return a string, and take a single parameter of the type of the property
    /// </summary>
    public string FormatMethod { get; }

    /// <summary>
    /// Do not quote the result
    /// </summary>
    public bool NoQuote { get; }
}

public interface IPropertyAttribute
{
    string Name { get; set; }
}

/// <summary>
/// Marker attribute for formatting the output
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class OutputPropertyFormatAttribute : OptionsToStringAttribute
{
    public OutputPropertyFormatAttribute() { }
}

/// <summary>
/// Marker attribute to should show only prefixLen or suffixLen characters and mask the rest
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class OutputPropertyMaskAttribute : OutputMaskAttribute, IPropertyAttribute
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
public sealed class OutputPropertyRegexAttribute : OutputRegexAttribute, IPropertyAttribute
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
public sealed class OutputPropertyLengthOnlyAttribute : OutputLengthOnlyAttribute, IPropertyAttribute
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
public sealed class OutputPropertyIgnoreAttribute : OutputIgnoreAttribute, IPropertyAttribute
{
    public OutputPropertyIgnoreAttribute(string name)
    {
        Name = name;
    }
    public string Name { get; set; } = "";
}

