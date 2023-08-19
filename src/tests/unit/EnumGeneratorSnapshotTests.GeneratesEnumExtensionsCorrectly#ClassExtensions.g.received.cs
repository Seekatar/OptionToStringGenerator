//HintName: ClassExtensions.g.cs

namespace Seekatar.ClassGenerators
{
    public static partial class ClassExtensions
    {
                public static string ToStringFast(this Colour value)
                    => value switch
                    {
                    _ => value.ToString(),
                };

    }
}