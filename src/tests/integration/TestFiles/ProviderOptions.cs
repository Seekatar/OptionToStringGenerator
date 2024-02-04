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

    public static string? MyDoubleNoQuotes(double? o)
    {
        if (o is null) return null;
        return o.Value.ToString("0.00");
    }
}
