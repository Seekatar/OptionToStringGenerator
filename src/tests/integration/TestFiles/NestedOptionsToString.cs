namespace Test;

using Seekatar.OptionToStringGenerator;

[OptionsToString(Indent = "    ")]
class InnerOption
{
    public InnerOption(string stringProp, int IntProp = 42, double DoubleProp = 3.14)
    {
        StringProp = stringProp;
    }

    [OutputMask(PrefixLen = 3)]
    public string StringProp { get; }
    public int IntProp { get; set; } = 42;
    public double DoubleProp { get; set; } = 3.14;
}

[OptionsToString(Indent = "      ")]
class InnerOptions
{
    public InnerOptions(string stringProp, int IntProp = 42, double DoubleProp = 3.14)
    {
        StringProp = stringProp;
    }

    [OutputMask(PrefixLen = 3)]
    public string StringProp { get; }
    public int IntProp { get; set; } = 42;
    public double DoubleProp { get; set; } = 6.022E23;
}

[OptionsToString]
class OuterOptions
{
    [OutputMask(SuffixLen = 6)]
    public string Secret { get; set; } = "hi mom this is the outer";

    [OutputFormatProvider(typeof(OuterOptions), nameof(MyFormatterOne), true)]
    public InnerOption InnerOption { get; set; } = new InnerOption("0inner");
    public int IntProp { get; set; } = 42;

    [OutputFormatProvider(typeof(OuterOptions), nameof(MyFormatter), true)]
    public IList<InnerOptions> InnerOptions { get; set; } = new List<InnerOptions>() {
        new InnerOptions("1inner") , new InnerOptions("2inner") , new InnerOptions("3inner")
    };
    [OutputFormatProvider(typeof(OuterOptions), nameof(MyFormatterNoQuotes), true)]
    public double DoubleProp { get; set; } = Math.PI;

    public static string? MyFormatterNoQuotes(double? o)
    {
        if (o is null) return null;
        return o.Value.ToString("0.00");
    }
    public static string? MyFormatterOne(InnerOption? o)
    {
        if (o is null) return null;
        return Environment.NewLine + "  " + o.OptionsToString();
    }
    public static string? MyFormatter(IList<Test.InnerOptions>? o)
    {
        if (o is null) return null;
        return Environment.NewLine + "    " + string.Join("      ", o.Select(p => p.OptionsToString())) ;
    }
}
