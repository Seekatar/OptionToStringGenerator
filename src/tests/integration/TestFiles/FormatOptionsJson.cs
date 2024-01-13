namespace Test;

using Seekatar;
using Seekatar.OptionToStringGenerator;

[OptionsToString(Json = true)]
public class FormatOptionsJson
{
    [OutputFormatProvider(typeof(FormatOptionsJson), nameof(MyFormatter))]
    public List<string> Secrets { get; set; } = new List<string> { "secret", "hushhush", "psssst" };

    public static string? MyFormatter(List<string> o)
    {
        if (o is null) return null;
        return string.Join(",", o.Select(s => Mask.MaskSuffix(s, 3)));
    }
}
