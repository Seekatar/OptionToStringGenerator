using Seekatar.OptionToStringGenerator;

namespace Test;

internal class ParentOptions
{
    [OutputIgnore]
    public string ParentIgnored{ get; set; } = "";
    [OutputMask]
    public string ParentSecret { get; set; } = "parent secret";
}

[OptionsToString]
internal class ChildOptions : ParentOptions
{
    [OutputMask(PrefixLen = 2)]
    public string ChildSecret { get; set; } = "child secret";
}

[OptionsToString(ExcludeParents = true)]
internal class ChildOnlyOptions : ParentOptions
{
    [OutputMask(PrefixLen = 2)]
    public string ChildSecret { get; set; } = "child secret";
}
