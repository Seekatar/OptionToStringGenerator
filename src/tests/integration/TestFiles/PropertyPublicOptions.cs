namespace Test;
using Seekatar.OptionToStringGenerator;

public class PropertyPublicOptions
{
    public class AClass
    {
        public string Name { get; set; } = "maybe this is secret";
        public override string ToString() => $"{nameof(AClass)}: {Name}";
    }

    public string PlainText { get; set; } = "hi mom";

    public char Why { get; set; } = 'Y';

    public int PlainInt { get; set; } = 42;

    public double PlainDouble { get; set; } = 3.141;

    public double PlainDecimal { get; set; } = 6.02;

    public DateTime PlainDateTime { get; set; } = new DateTime(2020, 1, 2, 3, 4, 5);

    public DateOnly PlainDatOnly { get; set; } = new DateOnly(2020, 1, 2);

    public TimeOnly PlainTimeOnly { get; set; } = new TimeOnly(12, 23, 2);

    public TimeSpan TimeSpan { get; set; } = new TimeSpan(1, 2, 3, 4, 5);

    public Guid UUID { get; set; } = Guid.Parse("6536b25c-3a45-48d8-8ea3-756e19f5bad1");

    public string? NullItem { get; set; }

    public AClass AnObject { get; set; } = new();

    [OutputRegex(Regex = @"AClass\:\s+(.*)")]
    public AClass AMaskedObject { get; set; } = new();

    [OutputMask]
    public string FullyMasked { get; set; } = "thisisasecret";

    [OutputMask(PrefixLen=3)]
    public string FirstThreeNotMasked { get; set; } = "abc1233435667";

    [OutputMask(SuffixLen=3)]
    public string LastThreeNotMasked { get; set; } = "abc1233435667";

    [OutputMask(PrefixLen = 3, SuffixLen=3)]
    public string FirstAndLastThreeNotMasked { get; set; } = "abc1233435667";

    [OutputMask(PrefixLen = 100)]
    public string NotMaskedSinceLongLength { get; set; } = "abc1233435667";

    [OutputLengthOnly]
    public string LengthOnly { get; set; } = "thisisasecretthatonlyshowsthelength";

    [OutputRegex(Regex="User Id=([^;]+).*Password=([^;]+)")]
    public string MaskUserAndPassword { get; set; } = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";

    [OutputRegex(Regex="User Id=([^;]+).*Password=([^;]+)",IgnoreCase=true)]
    public string MaskUserAndPasswordIgnoreCase { get; set; } = "Server=myServerAddress;Database=myDataBase;user Id=myUsername;Password=myPassword;";

    [OutputRegex(Regex = "User Id=([^;]+).*Password=([^;]+)")]
    public string RegexNotMatched { get; set; } = "Server=myServerAddress;Database=myDataBase;user Id=myUsername;Password=myPassword;";

    public ConsoleColor Color { get; set; } = ConsoleColor.Red;

    [OutputIgnore]
    public string IgnoreMe { get; set; } = "abc1233435667";
}

class PropertyPublicTest
{
    [OutputPropertyRegex(nameof(PropertyPublicOptions.AMaskedObject), Regex = @"AClass\:\s+(.*)")]
    [OutputPropertyMask(nameof(PropertyPublicOptions.FullyMasked))]
    [OutputPropertyMask(nameof(PropertyPublicOptions.FirstThreeNotMasked), PrefixLen = 3)]
    [OutputPropertyMask(nameof(PropertyPublicOptions.LastThreeNotMasked), SuffixLen = 3)]
    [OutputPropertyMask(nameof(PropertyPublicOptions.FirstAndLastThreeNotMasked), PrefixLen = 3, SuffixLen = 3)]
    [OutputPropertyMask(nameof(PropertyPublicOptions.NotMaskedSinceLongLength), PrefixLen = 100)]
    [OutputPropertyLengthOnly(nameof(PropertyPublicOptions.LengthOnly))]
    [OutputPropertyRegex(nameof(PropertyPublicOptions.MaskUserAndPassword), Regex = "User Id=([^;]+).*Password=([^;]+)")]
    [OutputPropertyRegex(nameof(PropertyPublicOptions.MaskUserAndPasswordIgnoreCase), Regex = "User Id=([^;]+).*Password=([^;]+)", IgnoreCase = true)]
    [OutputPropertyRegex(nameof(PropertyPublicOptions.RegexNotMatched), Regex = "User Id=([^;]+).*Password=([^;]+)")]
    [OutputPropertyIgnore(nameof(PropertyPublicOptions.IgnoreMe))]
    public PropertyPublicOptions PublicOptions { get; set; } = new PropertyPublicOptions();
}