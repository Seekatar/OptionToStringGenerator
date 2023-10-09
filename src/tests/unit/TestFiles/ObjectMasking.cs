namespace Test;
using Seekatar.OptionToStringGenerator;

[OptionsToString]
class ObjectMasking
{
    public ObjectMasking()
    {
        AnObject = new() { Name = "Server=myServerAddress;Database=myDataBase;user Id=myUsername;Password=myPassword;" };
    }

    [OutputRegex(Regex = "User Id=([^;]+).*Password=([^;]+)", IgnoreCase = true)]
    public PublicOptions.AClass AnObject { get; }
}
