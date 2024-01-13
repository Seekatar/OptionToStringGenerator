using System.Text.Json;

namespace Seekatar;

/// <summary>
/// Static methods for masking a object's value
/// </summary>
public static class Mask
{
    /// <summary>
    /// The string returned when the object is null
    /// </summary>
    public static string? NullLiteral { get; set; }

    private static string? CheckNullObject(object? o)
    {
        if (o is null) return null;
        return o.ToString() ?? "";
    }

    /// <summary>
    /// Mask the object's value to its length only
    /// </summary>
    /// <param name="o"></param>
    /// <param name="asJson"></param>
    /// <returns>String that is the numeric length, or JSON</returns>
    public static string? MaskLengthOnly(object? o, bool asJson = false)
    {
        var value = CheckNullObject(o);
        if (value == null) return NullLiteral;

        return asJson ? ($"{{ \"Len\": {(value).Length} }}") : ("Len = " + (value).Length.ToString());
    }

    /// <summary>
    /// Mask the value but show the last suffixLen characters
    /// </summary>
    /// <param name="o"></param>
    /// <param name="suffixLen">number of trailing characters to show in the clear</param>
    /// <param name="maskChar">masking character</param>
    /// <param name="asJson">Output value encoded as JSON</param>
    /// <returns></returns>
    public static string? MaskPrefix(object? o, int suffixLen, char maskChar = '*', bool asJson = false) => MaskPrefixSuffix(o, -1, suffixLen, maskChar, asJson);

    /// <summary>
    /// Mask the value but show the last prefixLen characters
    /// </summary>
    /// <param name="o"></param>
    /// <param name="prefixLen">number of leading characters to show in the clear</param>
    /// <param name="maskChar">masking character</param>
    /// <param name="asJson">Output value encoded as JSON</param>
    /// <returns></returns>
    public static string? MaskSuffix(object? o, int prefixLen, char maskChar = '*', bool asJson = false) => MaskPrefixSuffix(o, prefixLen, -1, maskChar, asJson);

    /// <summary>
    /// Mask the value replacing all characters with maskChar
    /// </summary>
    /// <param name="o"></param>
    /// <param name="maskChar">masking character</param>
    /// <param name="asJson">Output value encoded as JSON</param>
    /// <returns></returns>
    public static string? MaskAll(object? o, char maskChar = '*', bool asJson = false) => MaskPrefixSuffix(o, 0, 0, maskChar, asJson);

    /// <summary>
    /// Mask the value replacing all characters with maskChar except the first prefixLen and last suffixLen
    /// </summary>
    /// <param name="o"></param>
    /// <param name="prefixLen">number of leading characters to show in the clear</param>
    /// <param name="suffixLen">number of trailing characters to show in the clear</param>
    /// <param name="maskChar">masking character</param>
    /// <param name="asJson">Output value encoded as JSON</param>
    /// <returns></returns>
    public static string? MaskPrefixSuffix(object? o, int prefixLen, int suffixLen, char maskChar = '*', bool asJson = false)
    {
        if (prefixLen < 0 && suffixLen < 0) throw new ArgumentOutOfRangeException("prefixLen and suffixLen cannot both be less than zero");

        var value = CheckNullObject(o);
        if (value == null) return NullLiteral;

        var s = value;
        var middleLen = Math.Max(0, s.Length
                                    - (prefixLen >= 0 ? prefixLen : 0)
                                    - (suffixLen >= 0 ? suffixLen : 0));
        if (middleLen == 0) return asJson ? JsonSerializer.Serialize(s) : s;
        var middle = new string(maskChar, middleLen);

        var prefix = "";
        var suffix = "";
        if (prefixLen > 0 && prefixLen < s.Length)
        {
            prefix = s.Substring(0, prefixLen);
        }
        if (suffixLen > 0 && suffixLen < s.Length)
        {
            suffix = s.Substring(s.Length - suffixLen);
        }

        return asJson ? JsonSerializer.Serialize(prefix + middle + suffix) :  prefix + middle + suffix;
    }

    /// <summary>
    /// Mask a string replacing all the captures in a regex with mask
    /// </summary>
    /// <param name="o"></param>
    /// <param name="regex">A regex with one or more captures</param>
    /// <param name="ignoreCase">Ignore case in the regex</param>
    /// <param name="mask">Func to mask the capture, defaults to '***'</param>
    /// <param name="asJson">Output value encoded as JSON</param>
    /// <returns></returns>
    public static string? MaskRegex(object? o, string regex, bool ignoreCase = false, bool asJson = false, Func<string,string?>? mask = null)
    {
        var value = CheckNullObject(o);
        if (value == null) return NullLiteral;

        var r = new System.Text.RegularExpressions.Regex(regex, ignoreCase ? System.Text.RegularExpressions.RegexOptions.IgnoreCase : System.Text.RegularExpressions.RegexOptions.None);
        var s = value;
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
                    s = s.Replace(cc[j].ToString(), mask != null ? mask(cc[j].ToString()) : "***");
                }
            }
            m = m.NextMatch();
        }
        if (matchCount == 0) s = "***Regex no match***!";
        return asJson ? JsonSerializer.Serialize(s) : s;
    }

    private static string CheckNullQuote( string ?s, bool asJson)
    {
        if (s is null) return "null";
        if (asJson) return s;
        return "\"" + s + "\"";
    }

    /// <summary>
    /// Helper for formatting objects for output, called by generated code
    /// </summary>
    /// <param name="o">object to do ToString() on</param>
    /// <param name="lengthOnly">only show length</param>
    /// <param name="prefixLen">mask all but prefix</param>
    /// <param name="regex">Regex to mask</param>
    /// <param name="ignoreCase">ignore case on regex</param>
    /// <param name="maskChar">masking character</param>
    /// <param name="asJson">for lengthOnly, render as JSON</param>
    /// <returns></returns>
    public static string? Format<T>(T? o, bool lengthOnly = false, int prefixLen = -1, int suffixLen = -1, string? regex = null, bool ignoreCase = false, bool asJson = false, char maskChar = '*', Func<T?, string?>? formatMethod = null)
    {
        if (o is null) return "null";

        string value;
        if (formatMethod is not null)
            value = formatMethod(o) ?? "";
        else
            value = o.ToString() ?? "";
        if (lengthOnly) return MaskLengthOnly(o, asJson);

        if (prefixLen >= 0 || suffixLen >= 0)
        {
            return CheckNullQuote(MaskPrefixSuffix(o, prefixLen, suffixLen, maskChar, asJson), asJson);
        }

        if (regex is not null)
        {
            return CheckNullQuote(MaskRegex(o, regex, ignoreCase, asJson), asJson);
        }

        if (o is bool)
            return (value).ToLowerInvariant();

        if (o is string or char
            || o.GetType().IsClass
            || (asJson && (o is Guid or DateTime or TimeSpan
                  || o.GetType().IsEnum
                  || o.GetType().Name == "DateOnly" // .NET Standard 2.0 doesn't have these types, so can't use nameof
                  || o.GetType().Name == "TimeOnly"))
           )
            return asJson ? JsonSerializer.Serialize(value) : "\"" + value + "\"";

        if (o.GetType().IsPrimitive)
        {
            return value;
        }

        return asJson ? JsonSerializer.Serialize(value) : value;
    }
}
