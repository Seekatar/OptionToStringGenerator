# OptionToString Incremental Source Generator

This repo has an incremental source generator that generates a `ToString` method for types you want to dump out to the console or logs in a readable way. This is often used for dumping out objects used by IOptions or IConfiguration to log the configuration values for an application.

Big shout out to Andrew Lock and his [blog series](https://andrewlock.net/creating-a-source-generator-part-1-creating-an-incremental-source-generator/) on incremental source generators. I used that as a starting point for this project.

I followed his directions, which do lead you on a path that requires making changes to the original code. In particular for the last step to break out the `Attributes` into their own assembly.

In the initial generator, he injects the `Attributes` as code with these lines in the `Initialize` method of the generator, which is the typical method.

```csharp
context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
    "ClassExtensionsAttribute.g.cs",
    SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)));
```

He says this works fine unless someone uses `InternalsVisibleTo` to expose the internals of one assembly to another. He tried several things to solve this before coming up with a robust solution in [part 8](https://andrewlock.net/creating-a-source-generator-part-8-solving-the-source-generator-marker-attribute-problem-part2/) of his series. There's quite a bit of advanced csproj editing that he covers to get it to work. I applied similar changes and everything but the unit tests worked. After viewing his [repo](https://github.com/andrewlock/StronglyTypedId), I found his original unit test helper methods to build the code on-the-fly for the unit tests was different. After picking up those changes, the unit tests worked.

## Links to Documentation

These are links to the MS documentation for the symbol and methods I used in the generator.

[IPropertySymbol](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.ipropertysymbol?view=roslyn-dotnet-4.6.0)

- [GetMethod](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.ipropertysymbol.getmethod?view=roslyn-dotnet-4.6.0)

[ISymbol](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol?view=roslyn-dotnet-4.6.0)

- [GetAttributes](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol.getattributes?view=roslyn-dotnet-4.6.0)
- [ContainingNamespace](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol.containingnamespace?view=roslyn-dotnet-4.6.0)
- [DeclaredAccessibility](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol.declaredaccessibility?view=roslyn-dotnet-4.6.0)

## Links

- [Andrew Lock's blog series on incremental generators (Part 1)](https://andrewlock.net/creating-a-source-generator-part-1-creating-an-incremental-source-generator/)
- [Verify snapshot test tool](https://github.com/VerifyTests/Verify)
