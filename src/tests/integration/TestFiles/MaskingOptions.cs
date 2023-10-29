using Seekatar.OptionToStringGenerator;

namespace Test;

[OptionsToString()]
internal class MaskingOptions
{
    private const string alphabet = "abcdefghijklmnopqrstuvwxyz";

    [OutputMask]
    public string Masked { get; set; } = alphabet;

    [OutputMask(PrefixLen = 3)]
    public string MaskedPrefix { get; set; } = alphabet;

    [OutputMask(SuffixLen = 3)]
    public string MaskedSuffix { get; set; } = alphabet;

    [OutputMask(PrefixLen = 3, SuffixLen = 3)]
    public string MaskedPrefixSuffix { get; set; } = alphabet;

    [OutputMask(PrefixLen = 100)]
    public string MaskedPrefixTooBig { get; set; } = alphabet;

    [OutputMask(SuffixLen = 100)]
    public string MaskedSuffixTooBig { get; set; } = alphabet;

    [OutputMask(PrefixLen = 100, SuffixLen = 100)]
    public string MaskedBothTooBig { get; set; } = alphabet;

    [OutputMask(PrefixLen = 10, SuffixLen = 10)]
    public string Empty { get; set; } = "";

    [OutputMask(PrefixLen = -10, SuffixLen = -10)]
    public string Negative { get; set; } = alphabet;

    [OutputMask(PrefixLen = 0, SuffixLen = 0)]
    public string Zero { get; set; } = alphabet;
}

