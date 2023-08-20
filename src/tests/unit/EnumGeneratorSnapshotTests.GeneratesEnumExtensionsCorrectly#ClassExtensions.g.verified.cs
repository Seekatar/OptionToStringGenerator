//HintName: ClassExtensions.g.cs

namespace Seekatar.ClassGenerators
{
    public static partial class ClassExtensions
    {
        string Format(object o, bool lengthOnly = false, bool mask = false, int prefixLen = 0, bool connectionString = false)
        {
            if (lengthOnly) {
                 return "Len = " + o.ToString().Length.ToString();
            }
            else if (prefixLen > 0) {
                var s = o.ToString();
                if (prefixLen < s.Length) {
                    return "\"" + s.Substring(0, prefixLen) + new string('*', s.Length - prefixLen) + "\"";
                } else {
                    return "\"" + s + "\"";
                }
            } else if (mask) {
                return "\"" + new string('*', o.ToString().Length) + "\"";
            } else if (o is string) {
                return "\"" + o + "\"";
            } else {
                return o.ToString();
            }
        }
        public static string OptionsToString(this MyAppOptions o)
        {
            return $"""
                    MyAppOptions:
                      Name        : {Format(o.Name)}
                      Description : {Format(o.Description,false,false,0,false)}
                      Password    : {Format(o.Password,false,true,0,false)}
                      Secret      : {Format(o.Secret,true,false,0,false)}
                      Certificate : {Format(o.Certificate,false,false,3,false)}
                    """;
      }
    }
}