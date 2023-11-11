using Seekatar.OptionToStringGenerator;


namespace Test;

internal interface IOptionsSimple
{
    public string Secret { get; set; }
    public int RetryLimit { get; set; }
    public string ConnectionString { get; set; }
}

[OptionsToString]
internal class PropertySimple : IOptionsSimple
{
    [OutputMask]
    public string Secret { get; set; } = "Secret";

    public int RetryLimit { get; set; } = 5;

    [OutputRegex(Regex = "User Id=([^;]+).*Password=([^;]+)", IgnoreCase = true)]
    public string ConnectionString { get; set; } = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";
}

internal class PropertyInterface
{
    [OutputPropertyMask(nameof(IOptionsSimple.Secret))]
    [OutputPropertyRegex(nameof(IOptionsSimple.ConnectionString), Regex = "User Id=([^;]+).*Password=([^;]+)", IgnoreCase = true)]
    public IOptionsSimple? PropertySimple { get; set; }
}
