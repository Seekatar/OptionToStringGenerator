namespace Test;
using Seekatar.OptionToStringGenerator;

class OmClass
{
    public string Name { get; set; } = string.Empty;
    public override string ToString() => $"{nameof(OmClass)}: {Name}";
}

[OptionsToString]
class ObjectMasking
{
    public ObjectMasking()
    {
        AnObject = new() { Name = "Server=myServerAddress;Database=myDataBase;user Id=myUsername;Password=myPassword;" };
    }

    [OutputRegex(Regex = "User Id=([^;]+).*Password=([^;]+)", IgnoreCase = true)]
    public OmClass AnObject { get; }
}
