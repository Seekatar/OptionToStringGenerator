# OptionToString Incremental Source Generator

[![OptionToStringGenerator](https://github.com/Seekatar/OptionToStringGenerator/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Seekatar/OptionToStringGenerator/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/Seekatar/OptionToStringGenerator/branch/main/graph/badge.svg?token=X3J5MU9T3C)](https://codecov.io/gh/Seekatar/OptionToStringGenerator)

**Problem:** I have configuration class for use with [IOptions](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options) and I want to safely log out its values at runtime.

**Solution:** Use an incremental source generator to generate an extension method to get a string with masked values for the properties.

This package generates an `OptionToString`
extension method for your classes. By marking properties in the class you can control how the values are masked. It was created for dumping out objects used by [IOptions](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options) or [IConfiguration](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfiguration) when the application starts.

## Usage

1. Add the [OptionToString](https://www.nuget.org/packages/OptionToString/) NuGet package to your project.
2. Decorate a class with the `OptionToStringAttribute` attribute.
3. Optionally decorate properties with how you want them to be you want to dump out. If you don't decorate a property, its full text is dumped out.

### Example

Here's an example class of the various options with values set in the class for illustration purposes. The output follows.

```csharp
namespace integration;
using Seekatar.OptionToStringGenerator;

[OptionsToString]
public class PublicOptions
{
    public class AClass
    {
        public string Name { get; set; } = "maybe this is secret";
        public override string ToString() => $"{nameof(AClass)}: {Name}";
    }

    public string PlainText { get; set; } = "hi mom";

    public int PlainNumber { get; set; } = 42;

    public DateTime PlainDateTime { get; set; } = new DateTime(2020, 1, 1);

    public string? NullItem { get; set; }

    public AClass AnObject { get; set; } = new();

    [OutputRegex(Regex = @"AClass\:\s+(.*)")]
    public AClass AMaskedObject { get; set; } = new();

    [OutputMask]
    public string FullyMasked { get; set; } = "thisisasecret";

    [OutputMask(PrefixLen=3)]
    public string FirstThreeNotMasked { get; set; } = "abc1233435667";

    [OutputMask(PrefixLen = 100)]
    public string NotMaskedSinceLongLength { get; set; } = "abc1233435667";

    [OutputLengthOnly]
    public string LengthOnly { get; set; } = "thisisasecretthatonlyshowsthelength";

    [OutputRegex(Regex="User Id=([^;]+).*Password=([^;]+)")]
    public string MaskUserAndPassword { get; set; } = "...;User Id=myUsername;Password=myPassword;";

    [OutputRegex(Regex="User Id=([^;]+).*Password=([^;]+)",IgnoreCase=true)]
    public string MaskUserAndPasswordIngoreCase { get; set; } = "...;user Id=myUsername;Password=myPassword;";

    [OutputRegex(Regex = "User Id=([^;]+).*Password=([^;]+)")]
    public string RegexNotMatched { get; set; } = "...;user Id=myUsername;Password=myPassword;";

    [OutputIgnore]
    public string IgnoreMe { get; set; } = "abc1233435667";
}
// usage
var options = new PublicOptions();
_logger.LogInformation(options.OptionToString());
```

The output has the class name followed by an indented list of all the properties' values masked as specified.

```text
integration.PublicOptions:
  PlainText                     : "hi mom"
  PlainNumber                   : 42
  PlainDateTime                 : 1/1/2020 12:00:00 AM
  NullItem                      : null
  AnObject                      : AClass: maybe this is secret
  AMaskedObject                 : AClass: ***
  FullyMasked                   : "*************"
  FirstThreeNotMasked           : "abc**********"
  NotMaskedSinceLongLength      : "abc1233435667"
  LengthOnly                    : Len = 35
  MaskUserAndPassword           : ...;User Id=***;Password=***;
  MaskUserAndPasswordIngoreCase : ...;user Id=***;Password=***;
  RegexNotMatched               : ***!
```

### Notes

- All public properties are included by default and output as plain text.
- Use the `OutputIgnore` attribute to exclude a property.
- `ToString()` is called on the property's value, then the mask is applied. You can have a custom `ToString()` method on a class to format its output then it will be masked as the `AClass` example above.
- Only one `Output*` attribute is allowed per property. If more than one is set, you'll get a compile warning, and the last attribute set will be used.
- Regex strings with back slashes need to use a verbatim string or escape the back slashes (e.g.  `@"\s+"`  or `"\\s+"`).
- `OutputRegex` must have a `Regex` parameter, or you'll get a compile error.
- If the regex doesn't match the value, the output will be `***!` to indicate it didn't match.
## Implementation

Big shout out to Andrew Lock and his [blog series](https://andrewlock.net/creating-a-source-generator-part-1-creating-an-incremental-source-generator/) on incremental source generators. I used that as a starting point for this project.

His blog tells his story of building a source generator and you learn better ways to do things as you progress through the blog.

In particular, in the last entry he breaks out the `Attributes` into their own assembly. In the initial generator, he injects the `Attributes` as code with these lines in the `Initialize` method of the generator, which is the typical method like this:

```csharp
context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
    "ClassExtensionsAttribute.g.cs",
    SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)));
```

He says this works fine unless someone uses `InternalsVisibleTo` to expose the internals of one assembly to another. He tried several things to solve this before coming up with a robust solution in [part 8](https://andrewlock.net/creating-a-source-generator-part-8-solving-the-source-generator-marker-attribute-problem-part2/) of his series. There's quite a bit of advanced csproj editing that he covers to get it to work. I applied similar changes and everything but the unit tests worked. After viewing his [repo](https://github.com/andrewlock/StronglyTypedId), I found his original unit test helper methods to build the code on-the-fly for the unit tests was different. After picking up those changes, the unit tests worked.

### Basic Logic of OptionsToStringGenerator.Initialize()

This has the implementation of [IIncrementalGenerator](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.iincrementalgenerator).Initialize method. For this generator here's what I did:

1. Look for classes with at least one attribute (predicate, which must be very fast)
2. Look for ones with my `OptionToStringAttribute` (transform, which can be slower)
3. Execute() generates the code
    1. Take the syntax and get the semantic model of the class, extracting the name, accessibility, and list of properties with a `get`
    2. Generate the code for the extension method

## Roadmap

- [ ] More configurability of the output. Indent, mask character, mask len, etc.
- [ ] JSON output
- [ ] Ability to use ILogger with semantic logging
- [ ] Allow alternative method to get the string value of a property
- [ ] Allow passing in a format parameter string for ToString(), at least for dates, datetimes, and numbers
- [ ] Add option of postfix characters in masking
- [ ] Allow title to include properties of the class

## Links to Documentation

These are links to the MS documentation for the items I used in the generator.

[ISymbol](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol?view=roslyn-dotnet-4.6.0) -- Base class for all semantic symbols

[IPropertySymbol](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.ipropertysymbol?view=roslyn-dotnet-4.6.0) -- Semantic for the property

- [GetMethod](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.ipropertysymbol.getmethod?view=roslyn-dotnet-4.6.0) -- is it a {get}
- [DeclaredAccessibility](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol.declaredaccessibility) -- is it public?

[INamedTypeSymbol](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.inamedtypesymbol) -- More specific semantic for the class

- [GetAttributes](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol.getattributes?view=roslyn-dotnet-4.6.0)
- [ContainingNamespace](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol.containingnamespace?view=roslyn-dotnet-4.6.0)
- [DeclaredAccessibility](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol.declaredaccessibility?view=roslyn-dotnet-4.6.0)
- [GetMembers](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.inamespaceortypesymbol.getmembers) -- get all the members of the class

## Links

- [Andrew Lock's blog series on incremental generators (Part 1)](https://andrewlock.net/creating-a-source-generator-part-1-creating-an-incremental-source-generator/)
- [Verify snapshot test tool](https://github.com/VerifyTests/Verify)
