namespace Seekatar.OptionToStringGenerator;

public static class SourceGenerationHelper
{
    public const string Attribute =
        """
        namespace Seekatar.OptionToStringGenerator;

        [System.AttributeUsage(System.AttributeTargets.Class)]
        public class OptionsToStringAttribute : System.Attribute
        {
        }

        [System.AttributeUsage(System.AttributeTargets.Property)]
        public class OutputMaskAttribute : System.Attribute
        {
            public int PrefixLen { get; set; }
        }

        [System.AttributeUsage(System.AttributeTargets.Property)]
        public class OutputRegexAttribute : System.Attribute
        {
            public string Regex { get; set; }
            public bool IgnoreCase { get; set; }
        }
        
        [System.AttributeUsage(System.AttributeTargets.Property)]
        public class OutputLengthOnlyAttribute: System.Attribute
        {
        }
        
        [System.AttributeUsage(System.AttributeTargets.Property)]
        public class OutputIgnoreAttribute : System.Attribute
        {
        }
        """;
}