//#define SHOW_GENERATOR_ERRORS
using Seekatar.OptionToStringGenerator;

namespace Test;

#if SHOW_GENERATOR_ERRORS
[OptionsToString]
internal class ErrorTest
{
    [OutputRegex(Regex = "(b\ad regex")] // SEEK001
    public string BadRegEx { get; set; } = "";

    [OutputRegex()] // SEEK001
    public string MissingRegEx { get; set; } = "";

    [OutputRegex(Regex = "")]
    [OutputMask(PrefixLen = 3)]
    public string Password { get; set; } = "ThisIsSecret"; // SEEK002

}

public class FakeOptions
{
    public string Name { get; set; } = "";
}

[OptionsToString()]
public class NoPublicProperties // SEEK003
{

}

[OptionsToString(Title = "Hi{ThisDoesNotExist}")]
public class BadTitleOptions // SEEK004
{
    public string Name { get; set; } = "hi mom";
}

[OptionsToStringAttribute]
private class PrivateClassNotAllowed // SEEK005
{
    public string Name { get; set; } = "hi mom";
}
public class BadType2
{
    [OutputPropertyMask("BadName")] // SEEK006
    public FakeOptions? NoName2 { get; set; }

    [OutputPropertyMask("")] // SEEK007
    public FakeOptions? NoName { get; set; }

}

public class BadType
{
    [OutputPropertyMask(nameof(BadTitleOptions.Name))]
    public Guid BadGuid { get; set; } // SEEK008
    [OutputPropertyMask(nameof(BadTitleOptions.Name))]
    public DateTime BadDateTime { get; set; } // SEEK008
}


#endif
