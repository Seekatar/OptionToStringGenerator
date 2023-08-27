# OptionToString Incremental Source Generator

This repo has an incremental source generator that generates a `ToString` method for types you want to dump out to the console or logs in a readable way. This is often used for dumping out objects used by IOptions or IConfiguration to log the configuration values for an application.

Big shout out to Andrew Lock and his [blog series](https://andrewlock.net/creating-a-source-generator-part-1-creating-an-incremental-source-generator/) on incremental source generators. I used that as a starting point for this project.

## TODO

- [ ] NuGet with attributes
- [x] nullable

## Links to documentation

[IPropertySymbol](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.ipropertysymbol?view=roslyn-dotnet-4.6.0)

- [GetMethod](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.ipropertysymbol.getmethod?view=roslyn-dotnet-4.6.0)

[ISymbol](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol?view=roslyn-dotnet-4.6.0)

- [GetAttributes](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol.getattributes?view=roslyn-dotnet-4.6.0)
- [ContainingNamespace](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol.containingnamespace?view=roslyn-dotnet-4.6.0)
- [DeclaredAccessibility](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol.declaredaccessibility?view=roslyn-dotnet-4.6.0)

## Links

- [Andrew Lock's blog series on incremental generators (Part 1)](https://andrewlock.net/creating-a-source-generator-part-1-creating-an-incremental-source-generator/)
- [Verify snapshot test tool](https://github.com/VerifyTests/Verify)
