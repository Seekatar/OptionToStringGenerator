namespace Test;

using Seekatar;
using Seekatar.OptionToStringGenerator;

[OptionsToString]
public class FormatOptions
{
    [OutputFormat(ToStringFormat = "N0")]
    public int PlainInt { get; set; } = 423433;

    [OutputFormat(ToStringFormat = "0.00")]
    public double PlainDouble { get; set; } = 3.141;

    [OutputFormat(ToStringFormat = "U")]
    public DateTime PlainDateTime { get; set; } = new DateTime(2020, 1, 2, 3, 4, 5);

    [OutputFormat(ToStringFormat = "r")]
    public DateOnly PlainDateOnly { get; set; } = new DateOnly(2020, 1, 2);

    [OutputFormat(ToStringFormat = "hh:mm:ss")]
    public TimeOnly PlainTimeOnly { get; set; } = new TimeOnly(12, 23, 2);

    [OutputFormat(ToStringFormat = @"hh\:mm\:ss")]
    public TimeSpan TimeSpan { get; set; } = new TimeSpan(1, 2, 3, 4, 5);

    [OutputFormat(FormatMethod = "Test.FormatOptions.MyFormatter")]
    public List<string> Secrets { get; set; } = new List<string> { "secret", "hushhush", "psssst" };

    public static string? MyFormatter(List<string>? o)
    {
        if (o is null) return null;
        return string.Join(",", o.Select(s => Mask.MaskSuffix(s, 3)));
    }
}

