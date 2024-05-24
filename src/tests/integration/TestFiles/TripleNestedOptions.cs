namespace Test;

using Seekatar.OptionToStringGenerator;


// not needed [OptionsToString(Indent = "      ")]
class Child
{
    [OutputMask(SuffixLen = 6)]
    public string ChildSecret { get; set; } = "From the child";
}

[OptionsToString(Indent = "    ")]
class GrandchildA : Child
{
    [OutputMask(SuffixLen = 10)]
    public string GrandchildSecret { get; set; } = "From the grandchild";
}

[OptionsToString(Indent = "    ")]
class GrandchildB : Child
{
    [OutputMask(SuffixLen = 10)]
    public string GrandchildSecret { get; set; } = "From the grandchild";
}

[OptionsToString(Indent = "    ")]
class GrandchildC : Child
{
    [OutputMask(SuffixLen = 10)]
    public string GrandchildSecret { get; set; } = "From the grandchild";
    public static string FormatterC(GrandchildC? grandchild)
    {
        return grandchild?.OptionsToString() ?? string.Empty;
    }

}

[OptionsToString]
class Parent
{
    [OutputMask(SuffixLen = 6)]
    public string Secret { get; set; } = "From the parent";

    [OutputFormatProvider(typeof(Parent), nameof(Formatter), noQuote: true)]
    public GrandchildA Grandchild { get; set; } = new GrandchildA();

    public static string Formatter(GrandchildA? grandchild)
    {
        return grandchild?.OptionsToString() ?? string.Empty;
    }

    [OutputFormatProvider(typeof(Parent), nameof(FormatterB), noQuote: true)]
    public GrandchildB GrandchildB { get; set; } = new GrandchildB();

    public static string FormatterB(GrandchildB? grandchild)
    {
        return grandchild?.OptionsToString() ?? string.Empty;
    }

    [OutputFormatProvider(typeof(GrandchildC), nameof(GrandchildC.FormatterC), noQuote:true)]
    public GrandchildC? GrandchildC { get; set; } = null;
}
