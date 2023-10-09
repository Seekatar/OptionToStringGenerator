using System.Runtime.CompilerServices;

namespace Seekatar.OptionToStringGenerator;

/// <summary>
/// Marker attribute to indicate a OptionToString() extension method should be generated
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class OptionsToStringAttribute : Attribute
{
    public string Indent { get; set; } = "  ";
    public string Separator { get; set; } = ":";
    public bool Json { get; set; } = false;
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
    public static string Format(object? o, bool lengthOnly = false, int prefixLen = -1, string? regex = null, bool ignoreCase = false, bool asJson = false)
    {
        if (o is null) return "null";

        var value = o.ToString() ?? "";
        if (lengthOnly) return asJson ? ($"{{ \"Len\": {(value).Length} }}") : ("Len = " + (value).Length.ToString());

        if (prefixLen >= 0)
        {
            var s = (value);
            if (prefixLen < s.Length)
            {
                return "\"" + s.Substring(0, prefixLen) + new string('*', s.Length - prefixLen) + "\"";
            }
            else
            {
                return "\"" + s + "\"";
            }
        }

        if (regex is not null)
        {
            var r = new System.Text.RegularExpressions.Regex(regex, ignoreCase ? System.Text.RegularExpressions.RegexOptions.IgnoreCase : System.Text.RegularExpressions.RegexOptions.None);
            var s = (value);
            var matchCount = 0;
            var m = r.Match(s);
            while (m.Success)
            {
                matchCount++;
                for (int i = 1; i < m.Groups.Count; i++)
                {
                    var cc = m.Groups[i].Captures;
                    for (int j = 0; j < cc.Count; j++)
                    {
                        s = s.Replace(cc[j].ToString(), "***");
                    }
                }
                m = m.NextMatch();
            }
            return $"\"{(matchCount > 0 ? s : "***!")}\""; // if not matches, return mask
        }

        if (o is bool) 
            return (value).ToLowerInvariant();

        if (o is string or char 
            || o.GetType().IsClass 
            || (asJson && (o is Guid or DateTime
                  || o.GetType().Name == "DateOnly" || o.GetType().Name == "TimeOnly" )) // can't use these types in .NET Standard 2.0
           )
            return "\"" + value + "\"";

        if (o.GetType().IsPrimitive)
        {
            return value;
        }


        return value;
    }
}

/// <summary>
/// Marker attribute to should show only prefixLen characters and mask the rest
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class OutputMaskAttribute : Attribute
{
    /// <summary>
    /// Number of unmasked characters at the start of the string
    /// </summary>
    public int PrefixLen { get; set; }
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
