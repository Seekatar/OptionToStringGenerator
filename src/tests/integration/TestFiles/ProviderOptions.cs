namespace Test;

using Seekatar.OptionToStringGenerator;

[OptionsToString]
class ProviderOptions
{
    [OutputFormatProvider(typeof(ProviderOptions), nameof(MyDoubleNoQuotes), true)]
    public double DoubleProp { get; set; } = Math.PI;

    [OutputFormatProvider(typeof(ProviderOptions), nameof(MyDoubleNoQuotes), true)]
    public double? DoublePropNullable { get; set; } = Math.PI;

    [OutputFormatProvider(typeof(ProviderOptions), nameof(MyDoubleNoQuotes), true)]
    public double? DoublePropNullableNull { get; set; }

    [OutputFormatProvider(typeof(ProviderOptions), nameof(MyStringQuotes))]
    public string? StringPropNullable { get; set; } = "This is a long string";

    [OutputFormatProvider(typeof(ProviderOptions), nameof(MyStringQuotes), true)]
    public string? StringPropNullableNull { get; set; } = "This is a longer string";

    public static string? MyStringQuotes(string? o)
    {
        if (o is null) return null;
        return o.Replace("i","ZZZ");
    }

    public static string? MyDoubleNoQuotes(double? o)
    {
        if (o is null) return null;
        return o.Value.ToString("0.000");
    }
}
