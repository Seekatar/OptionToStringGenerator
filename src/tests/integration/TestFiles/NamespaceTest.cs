namespace Test.Next.Level;

using Seekatar.OptionToStringGenerator;

[OptionsToString]
internal class NamespaceTest
{
    public string StringProperty { get; set; } = "This is a string";
    public int TheAnswerToLifeTheUniverseAndEverything { get; set; } = 42;
}
