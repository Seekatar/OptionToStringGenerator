namespace Test;

using Seekatar.OptionToStringGenerator;

class Wrapper
{
    [OptionsToString]
    internal class NestedOptions
    {
        public int IntProp { get; set; } = 42;
        public string StringProp { get; set; } = "hi mom";
    }

    public static string GetOptionsString()
    {
        return new NestedOptions().OptionsToString();
    }
}