using Seekatar.OptionToStringGenerator;

namespace Test;

internal class PropertyParentOptions
{
    public string ParentIgnored { get; set; } = "";
    public string ParentSecret { get; set; } = "parent secret";
}

internal class PropertyChildOptions : PropertyParentOptions
{
    public string ChildSecret { get; set; } = "child secret";
}

internal class PropertyChildOnlyOptions : PropertyParentOptions
{
    public string ChildSecret { get; set; } = "child secret";
}


internal class PropertyInheritance
{
    [OutputPropertyIgnore(nameof(PropertyChildOptions.ParentIgnored))]
    [OutputPropertyMask(nameof(PropertyChildOptions.ParentSecret))]
    [OutputPropertyMask(nameof(PropertyChildOptions.ChildSecret), PrefixLen = 2)]
    public PropertyChildOptions ChildOptions { get; set; } = new PropertyChildOptions();

    [OutputPropertyFormat(ExcludeParents = true)]
    [OutputPropertyMask(nameof(PropertyChildOnlyOptions.ChildSecret), PrefixLen = 2)]
    public PropertyChildOnlyOptions ChildOnlyOptions { get; set; } = new PropertyChildOnlyOptions();
}
