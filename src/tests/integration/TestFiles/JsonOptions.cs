#nullable enable
using System;
using System.IO;
using Seekatar.OptionToStringGenerator;

namespace Test;

[OptionsToString(Json = true)]
class JsonOptions
{
    public string Name { get; set; } = "hi mom";

    public char Why { get; set; } = 'Y';

    public int PlainInt { get; set; } = 42;

    public double PlainDouble { get; set; } = 3.141;

    public double PlainDecimal { get; set; } = 6.02;

    public DateTime PlainDateTime { get; set; } = new DateTime(2020, 1, 2, 3, 4, 5);

    public DateOnly PlainDatOnly { get; set; } = new DateOnly(2020, 1, 2);

    public TimeOnly PlainTimeOnly { get; set; } = new TimeOnly(12, 23, 2);

    public TimeSpan TimeSpan { get; set; } = new TimeSpan(1, 2, 3, 4, 5);

    public Guid UUID { get; set; } = Guid.Parse("6536b25c-3a45-48d8-8ea3-756e19f5bad1");

    public string? NullName { get; set; }

    public bool YesNo { get; set; }

    [OutputMask]
    public string Password { get; set; } = "thisisasecret";

    [OutputIgnore]
    public string IgnoreMe { get; set; } = "abc1233435667";

    [OutputMask(PrefixLen = 3)]
    public string Certificate { get; set; } = "abc1233435667";

    [OutputMask(PrefixLen = 30)]
    public string CertificateShort { get; set; } = "abc1233435667";

    [OutputLengthOnly]
    public string Secret { get; set; } = "thisisasecretthatonlyshowsthelength";

    [OutputRegex(Regex = "User Id=([^;]+).*Password=([^;]+)")]
    public string ConnectionString { get; set; } = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";

    [OutputRegex(Regex = "User Id=([^;]+).*Password=([^;]+)", IgnoreCase = true)]
    public string AnotherConnectionString { get; set; } = "Server=myServerAddress;Database=myDataBase;user Id=myUsername;Password=myPassword;";

    public FileAttributes AnEum { get; set; } = FileAttributes.Hidden | FileAttributes.ReadOnly;

    public string SpecialCharacters { get {
            string printableCharacters = "";
            for (int i = 32; i < 127; i++)
            {
                printableCharacters += (char)i;
            }
            return printableCharacters; 
        } }

    [OutputMask(PrefixLen = 3, SuffixLen = 3)]
    public string NoMiddle { get; set; } = "%$^%^#";

    [OutputRegex(Regex = "User Id=([^;]+).*Password=([^;]+)", IgnoreCase = true)]
    public string RegexNoMatch { get; set; } = "%$^%^#";
}
