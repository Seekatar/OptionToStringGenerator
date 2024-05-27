namespace Test;

using Seekatar.OptionToStringGenerator;
using System.ComponentModel.DataAnnotations;


class Child
{
    [OutputMask(SuffixLen = 6)]
    public string ChildSecret { get; set; } = "From the child";
}

[OptionsToString()]
class GrandchildA : Child
{
    [Required]
    [OutputMask(SuffixLen = 10)]
    public string GrandchildSecret { get; set; } = "From the grandchild";
}

[OptionsToString()]
class GrandchildB : Child
{
    [Required]
    [OutputMask(SuffixLen = 10)]
    public string GrandchildSecret { get; set; } = "From the grandchild";
}

[OptionsToString()]
class GrandchildC : Child
{
    [OutputMask(SuffixLen = 10)]
    [Required]
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

    public GrandchildA Grandchild { get; set; } = new GrandchildA();

    public GrandchildB GrandchildB { get; set; } = new GrandchildB();

    public GrandchildC? GrandchildC { get; set; } = null;
}

[OptionsToString]
class NestedOptions
{
    [OutputMask(PrefixLen = 3)]
    public string NestedSecret { get; set; } = "ChildSecret";
}

[OptionsToString]
class ParentOfNested
{
    [OutputMask(PrefixLen = 3)]
    public string Secret { get; set; } = "ParentSecret";

    public NestedOptions Nested { get; set; } = new();
}
