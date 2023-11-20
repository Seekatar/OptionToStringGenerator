namespace Test;
using Seekatar.OptionToStringGenerator;

[OptionsToString]
class NegativeBadOptions
{
    // shows warning about two attributes
    [OutputLengthOnly]
    [OutputRegex(Regex = "User Id=([^;]+).*Password=([^;]+)", IgnoreCase = true)]
    public string Name { get; set; } = "hi mom";

    private string NotShown1 { get; set; } = "bye mom";
    protected string NotShown2 { get; set; } = "bye mom";
    internal string NotShown3 { get; set; } = "bye mom";
    string NotShown4 { get; set; } = "bye mom";
}
