using Seekatar.OptionToStringGenerator;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test;

// mimic the PublicOptions
public class PropertyPublicOptions
{
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
    public PublicOptions.AClass AnObject { get; set; } = new();
    public PublicOptions.AClass AMaskedObject { get; set; } = new();
    public string FullyMasked { get; set; } = "thisisasecret";
    public string FirstThreeNotMasked { get; set; } = "abc1233435667";
    public string LastThreeNotMasked { get; set; } = "abc1233435667";
    public string FirstAndLastThreeNotMasked { get; set; } = "abc1233435667";
    public string NotMaskedSinceLongLength { get; set; } = "abc1233435667";
    public string LengthOnly { get; set; } = "thisisasecretthatonlyshowsthelength";
    public string MaskUserAndPassword { get; set; } = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";
    public string MaskUserAndPasswordIgnoreCase { get; set; } = "Server=myServerAddress;Database=myDataBase;user Id=myUsername;Password=myPassword;";
    public string RegexNotMatched { get; set; } = "Server=myServerAddress;Database=myDataBase;user Id=myUsername;Password=myPassword;";
    public ConsoleColor Color { get; set; } = ConsoleColor.Red;
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
    [MemberNotNull(nameof(PublicOptions))]
    public PropertyPublicOptions? PublicOptions { get; set; } = new PropertyPublicOptions();
}