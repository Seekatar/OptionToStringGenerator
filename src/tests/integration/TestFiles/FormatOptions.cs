#nullable enable
namespace Test;

using System;
using Seekatar;
using Seekatar.OptionToStringGenerator;
using System.Collections.Generic;
using System.Linq;

[OptionsToString]
public class FormatOptions
{
    [OutputFormatToString("N0")]
    public int PlainInt { get; set; } = 423433;

    [OutputFormatToString("0.00")]
    public double PlainDouble { get; set; } = 3.141;

    [OutputFormatToString("R")]
    public DateTime PlainDateTime { get; set; } = new DateTime(2020, 1, 2, 3, 4, 5);

    [OutputFormatToString("r")]
    public DateOnly PlainDateOnly { get; set; } = new DateOnly(2020, 1, 2);

    [OutputFormatToString("hh:mm:ss")]
    public TimeOnly PlainTimeOnly { get; set; } = new TimeOnly(12, 23, 2);

    [OutputFormatToString(@"hh\:mm\:ss")]
    public TimeSpan TimeSpan { get; set; } = new TimeSpan(1, 2, 3, 4, 5);

    [OutputFormatProvider(typeof(FormatOptions), nameof(MyFormatter))]
    public List<string> Secrets { get; set; } = new List<string> { "secret", "hushhush", "psssst" };

    public static string? MyFormatter(List<string>? o)
    {
        if (o is null) return null;
        return string.Join(",", o.Select(s => Mask.MaskSuffix(s, 3)));
    }
}

