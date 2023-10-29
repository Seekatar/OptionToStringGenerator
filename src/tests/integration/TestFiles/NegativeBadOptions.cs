namespace Test;
using Seekatar.OptionToStringGenerator;

[OptionsToString]
class NegativeBadOptions
{
    // shows warning about two attributes
    [OutputRegex(Regex = "User Id=([^;]+).*Password=([^;]+)", IgnoreCase = true)]
    [OutputLengthOnly]
    public string Name { get; set; } = "hi mom";

    private string NoteShown1 { get; set; } = "bye mom";
    protected string NoteShown2 { get; set; } = "bye mom";
    internal string NoteShown3 { get; set; } = "bye mom";
    string NoteShown4 { get; set; } = "bye mom";
}
