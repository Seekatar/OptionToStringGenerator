//HintName: ClassExtensions.g.cs
namespace Seekatar.ClassGenerators
{
    public static partial class ClassExtensions
    {
        string Format(object o, bool lengthOnly = false, int prefixLen = -1, string? regex = null, bool ignoreCase = false)
        {
            if ( o is null ) return "<null>";

            if (lengthOnly) return "Len = " + o.ToString().Length.ToString();

            if (prefixLen >= 0) {
                var s = o.ToString();
                if (prefixLen < s.Length) {
                    return "\"" + s.Substring(0, prefixLen) + new string('*', s.Length - prefixLen) + "\"";
                } else {
                    return "\"" + s + "\"";
                }
            } 

            if (regex is not null) {
                var r = new System.Text.RegularExpressions.Regex(regex);
                var s = o.ToString();
                var m = r.Match(s);
                while (m.Success) {
                    for ( int i = 1; i < m.Groups.Count; i++ ) {
                        var cc = m.Groups[i].Captures;
                        for ( int j = 0; j < cc.Count; j++ ) {
                            s = s.Replace(cc[j].ToString(), "***");
                        }
                    }
                    m = m.NextMatch();
                }
                return s;
            } 

            if (o is string) 
                return "\"" + o + "\"";
            else 
                return o.ToString();
        }
        public static string OptionsToString(this MyAppOptions o)
        {
            return $"""
                    MyAppOptions:
                      Name     : {Format(o.Name)}
                    """;
        }
    }
}