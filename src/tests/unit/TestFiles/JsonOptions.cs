using Seekatar.OptionToStringGenerator;

namespace Test;

[OptionsToString(Json = true)]
class JsonOptions
{
    public string Name { get; set; } = "hi mom";

    public string? NullName { get; set; }

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
}
