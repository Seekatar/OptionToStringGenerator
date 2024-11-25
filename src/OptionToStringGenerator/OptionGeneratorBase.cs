using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

using static Seekatar.OptionToStringGenerator.DiagnosticTemplates.Ids;
namespace Seekatar.OptionToStringGenerator;

public abstract class OptionGeneratorBase<TSyntax,TGeneratedItem> : IIncrementalGenerator
    where TSyntax : MemberDeclarationSyntax
    where TGeneratedItem : ItemToGenerate
{
    public abstract void Initialize(IncrementalGeneratorInitializationContext context);

    protected static bool HasAttribute(GeneratorSyntaxContext context, string fullAttributeName, SyntaxList<AttributeListSyntax> attributeLists)
    {
        // attribute list
        // IdentifierNameSyntax has Identifier Token
        // loop through all the attributes
        foreach (AttributeListSyntax attributeListSyntax in attributeLists)
        {
            foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                {
                    // weird, we couldn't get the symbol, ignore it
                    continue;
                }

                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                string fullName = attributeContainingTypeSymbol.ToDisplayString();

                // Is the attribute my attribute?
                if (fullName == fullAttributeName)
                {
                    // return the property
                    return true;
                }
            }
        }

        // we didn't find the attribute we were looking for
        return false;
    }

    protected void Execute(string sourceName, Compilation compilation, ImmutableArray<TSyntax> properties, SourceProductionContext context)
    {
        if (properties.IsDefaultOrEmpty)
        {
            // nothing to do yet
            return;
        }

        // I'm not sure if this is actually necessary, but `[LoggerMessage]` does it, so seems like a good idea!
        IEnumerable<TSyntax> distinctProperties = properties.Distinct();

        // Convert each T to an PropertyToGenerate
        List<TGeneratedItem> propertiesToGenerate = GetTypesToGenerate(compilation, distinctProperties, context.CancellationToken, context);

        // If there were errors in the T, we won't create an
        // PropertyToGenerate for it, so make sure we have something to generate
        if (propertiesToGenerate.Count > 0)
        {
            // generate the source code and add it to the output
            string result = GenerateExtensionClass(propertiesToGenerate, compilation, context);
            context.AddSource(sourceName+".g.cs", SourceText.From(result, Encoding.UTF8));
        }
    }
    protected abstract List<TGeneratedItem> GetTypesToGenerate(Compilation compilation, IEnumerable<TSyntax> distinctProperties, CancellationToken token, SourceProductionContext context);

    protected abstract ImmutableArray<AttributeData> AttributesForMember(IPropertySymbol symbol, TGeneratedItem propertyToGenerate);

    protected List<IPropertySymbol> GetAllPublicProperties(INamedTypeSymbol classSymbol, bool? excludeParent, bool? sort = null, List<IPropertySymbol>? members = null)
    {
        if (members is null)
        {
            members = new List<IPropertySymbol>();
        }
        var classMembers = classSymbol.GetMembers();
        foreach (ISymbol member in classMembers)
        {
            if (member is IPropertySymbol property
                && property.GetMethod is not null
                && property.DeclaredAccessibility == Accessibility.Public)
            {
                members.Add(property);
            }
        }

        if (!(excludeParent ?? false) && classSymbol.BaseType is INamedTypeSymbol baseType && baseType.Name != nameof(Object))
        {
            GetAllPublicProperties(baseType, excludeParent:false, sort:false, members);
        }

        if (sort ?? false) // only set sort to true on the top level call
        {
                        members = members.OrderBy(m => m.Name).ToList();
        }
        return members;
    }


    // AllIntefaces work for strings, array, but not IList or List???
    ITypeSymbol GetIListTypeArgument(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol &&
            namedTypeSymbol!.OriginalDefinition.ToDisplayString().EndsWith("IList<>"))
        {
            return namedTypeSymbol!.TypeArguments!.FirstOrDefault()!;
        }

        var ienumerableInterface = typeSymbol.AllInterfaces
            .FirstOrDefault(i => i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>");

        if (ienumerableInterface != null && ienumerableInterface is INamedTypeSymbol namedTypeSymbol2)
        {
            return namedTypeSymbol2.TypeArguments.FirstOrDefault();
        }

        return null;
    }

    protected string GenerateExtensionClass(List<TGeneratedItem> itemsToGenerate, Compilation compilation, SourceProductionContext context)
    {
        var sb = new StringBuilder();
        sb.Append("""
                    #nullable enable
                    using static Seekatar.Mask;
                    namespace Seekatar.OptionToStringGenerator
                    {
                        public static partial class ClassExtensions
                        {

                    """);
        foreach (var itemToGenerate in itemsToGenerate)
        {


            var className = itemToGenerate.Name;
            var classAccessibility = itemToGenerate.Accessibility;
            var members = itemToGenerate.Values;
            var formatAttribute = itemToGenerate.GetFormat();

            if (itemToGenerate.Accessibility == "private")
            {
                context.Report(SEEK005, itemToGenerate.Location, itemToGenerate.Name);
                continue;
            }

            int maxLen = 0;
            foreach (var member in members)
            {
                if (member.Name.Length > maxLen)
                {
                    maxLen = member.Name.Length;
                }
            }

            var nameSuffix = ":";
            var indent = "  ";
            var separator = ":";
            var nameQuote = "";
            var jsonClose = "";
            var trailingComma = "";
            var haveJson = false;
            var title = className;
            var titleText = "";

            if (formatAttribute is not null)
            {
                foreach (var n in formatAttribute.NamedArguments)
                {
                    if (n.Key == nameof(OptionsToStringAttribute.Json)
                        && n.Value.Value is not null
                        && (bool)n.Value.Value)
                    {
                        haveJson = true;
                        separator = " :";
                        nameQuote = "\"\"";
                        jsonClose = """
                                  }}
                                }}
                                """;
                        maxLen += 4; // for the quotes
                        trailingComma = ",";
                        haveJson = true;
                    }
                    else if (n.Key == nameof(OptionsToStringAttribute.Title)
                        && n.Value.Value is not null)
                    {
                        Regex regex = new(@"\{([^{}]+)\}", RegexOptions.Compiled);
                        // loop over all the  regex matches, and see if the string is a member of the property
                        var titleString = n.Value.Value.ToString();
                        var matches = regex.Matches(titleString);
                        foreach (Match match in matches)
                        {
                            var memberName = match.Groups[1].Value;
                            var member = members.Where(m => m.Name == memberName).FirstOrDefault();
                            if (member is null)
                            {
                                if (formatAttribute.ApplicationSyntaxReference is not null)
                                {
                                    context.Report(SEEK004, Location.Create(formatAttribute.ApplicationSyntaxReference.SyntaxTree, formatAttribute.ApplicationSyntaxReference.Span), memberName, itemToGenerate.Name);
                                }
                                else
                                {
                                    context.Report(SEEK004, itemToGenerate.Location, memberName, itemToGenerate.Name);
                                }

                                titleString = titleString.Replace($"{{{memberName}}}", memberName);
                            }
                            else
                            {
                                titleString = titleString.Replace($"{{{memberName}}}", $"{{o?.{memberName}}}");
                            }
                        }
                        title = titleString;
                    }
                    else if (!haveJson)
                    {
                        if (n.Key == nameof(OptionsToStringAttribute.Indent) && n.Value.Value is not null)
                        {
                            indent = n.Value.Value.ToString();
                        }
                        else if (n.Key == nameof(OptionsToStringAttribute.Separator) && n.Value.Value is not null)
                        {
                            separator = n.Value.Value.ToString();
                        }
                    }
                    }
                }

            if (haveJson)
            {
                titleText = $$$"""
                              {{
                                {{{nameQuote}}}{{{title}}}{{{nameQuote}}} {{{nameSuffix}}} {{
                              """;
            }
            else
            {
                titleText = $"{title}{{titleSuffix}}{nameSuffix}";
            }

            // method signature
            sb.Append($"        {classAccessibility} static string OptionsToString(this ")
              .Append(className)
              .Append($$""""
                      ? o, string extraIndent = "", string titleSuffix = "")
                              {
                                  return $@"
                      """");

            sb.Append($"{titleText}").AppendLine();

            if (!members.Any())
            {
                context.Report(SEEK003, itemToGenerate.Location);
                sb.AppendLine($"{{extraIndent}}{indent}No properties to display");
            }

            // each property
            string format = $"{{{{extraIndent}}}}{indent}{{0,-{maxLen}}} {separator} {{{{Format(o?.";
            int j = 0;
            foreach (var member in members)
            {
                if (++j == members.Count)
                    trailingComma = "";

                bool ignored = false;
                ImmutableArray<AttributeData> attributes = AttributesForMember(member, itemToGenerate);
                var formatParameters = haveJson ? $",asJson:{haveJson.ToString().ToLowerInvariant()}" : "";
                var attributeCount = 0;
                for (int i = 0; i < attributes.Length; i++)
                {
                    var attribute = attributes[i];
                    if (attribute.AttributeClass?.ContainingNamespace?.ToString() == "Seekatar.OptionToStringGenerator")
                    {
                        if (attributeCount > 0)
                        {
                            context.Report(SEEK002, member.Locations[0]);
                            continue;
                        }

                        attributeCount++;
                        if (attribute.AttributeClass?.Name.EndsWith("IgnoreAttribute") ?? false)
                            ignored = true;
                        else if (attribute.AttributeClass?.Name.EndsWith("MaskAttribute") ?? false)
                        {
                            var prefixLen = "0";
                            var suffixLen = "0";
                            for (int k = 0; k < attribute.NamedArguments.Length; k++)
                            {
                                if (attribute.NamedArguments[k].Key == nameof(OutputMaskAttribute.PrefixLen))
                                    prefixLen = attribute.NamedArguments[k].Value.Value?.ToString() ?? "0";
                                else if (attribute.NamedArguments[k].Key == nameof(OutputMaskAttribute.SuffixLen))
                                    suffixLen = attribute.NamedArguments[k].Value.Value?.ToString() ?? "0";
                            }
                            formatParameters += ",prefixLen:" + prefixLen + ",suffixLen:" + suffixLen;
                        }
                        else if (attribute.AttributeClass?.Name.EndsWith("LengthOnlyAttribute") ?? false)
                            formatParameters += $",lengthOnly:true";
                        else if (attribute.AttributeClass?.Name.EndsWith("RegexAttribute") ?? false)
                        {
                            var regexOk = false;
                            var message = "You must specify a regex parameter";
                            foreach (var n in attribute.NamedArguments)
                            {
                                if (n.Key == "Regex" && n.Value.Value is not null)
                                {
                                    var regex = n.Value.Value.ToString().Replace("\\", "\\\\");
                                    try
                                    {
                                        var r = new Regex(regex);
                                        formatParameters += ",regex:\"" + regex + "\"";
                                        regexOk = true;
                                    }
                                    catch (ArgumentException e)
                                    {
                                        message = e.Message;
                                        break;
                                    }
                                }
                                else if (n.Key == "IgnoreCase" && n.Value.Value is not null)
                                {
                                    formatParameters += ",ignoreCase:" + n.Value.Value.ToString().ToLowerInvariant();
                                }
                            }
                            if (!regexOk)
                            {
                                if (attribute.ApplicationSyntaxReference is not null)
                                {
                                    context.Report(SEEK001, Location.Create(attribute.ApplicationSyntaxReference.SyntaxTree, attribute.ApplicationSyntaxReference.Span), message);
                                }
                                else
                                {
                                    context.Report(SEEK001, member.Locations[0], message);
                                }
                            }
                        }
                        if (attribute.AttributeClass?.Name.EndsWith("ToStringAttribute") ?? false)
                        {
                            formatParameters += $",formatMethod:(o) => o?.ToString(\"{(attribute.ConstructorArguments[0].Value! as string)!.Replace("\\", "\\\\")}\")";
                        }
                        if (attribute.AttributeClass?.Name.EndsWith("FormatProviderAttribute") ?? false)
                        {
                            var formatProvider = (attribute.ConstructorArguments[0].Value! as ITypeSymbol)!;
                            var formatMethod = (attribute.ConstructorArguments[1].Value! as string)!;
                            var noQuote = (attribute.ConstructorArguments[2].Value! as bool?) ?? false;
                            ValidateTagProvider(formatProvider, formatMethod, member.Type, member.Type, member.Locations[0], compilation, context);
                            formatParameters += $",formatMethod:(o) => {formatProvider}.{formatMethod}(o),noQuote:{noQuote.ToString().ToLowerInvariant()}";
                        }
                    }
                }

                // nested Options?
                if (attributeCount == 0
                    && member.Type.TypeKind == TypeKind.Class
                    && member.Type.SpecialType != SpecialType.System_String
                    && member.Type.GetAttributes().FirstOrDefault(o => string.Equals(o.AttributeClass?.Name, nameof(OptionsToStringAttribute))
                                                                   && string.Equals(o.AttributeClass?.ContainingNamespace?.ToString(), "Seekatar.OptionToStringGenerator")) != null)
                {
                    Debug.WriteLine("Found OptionsToStringAttribute nested");
                    formatParameters += $",formatMethod:(o) => o?.OptionsToString(\"{indent}\") ?? \"null\",noQuote:true";
                } 
                else if (IsEnumerableOfOptionToString(member))
                {
                    var doubleIndent = indent + indent;
                    Debug.WriteLine("Found OptionsToStringAttribute nested");
                    formatParameters += ",formatMethod:(o) => { int i = 0; return Environment.NewLine + $\"{extraIndent}" + doubleIndent+ "Count: {o.Count()}\" + Environment.NewLine + $\"{extraIndent}" + doubleIndent+ "\"+ string.Join($\"{extraIndent}" + doubleIndent + "\", o.Select( oo => oo?.OptionsToString(\""+doubleIndent + "\"+extraIndent, titleSuffix:$\"[{i++}]\") ?? \"null\"));},noQuote:true";
                }
                else if (IsDictionaryOfOptionToString(member))
                {
                    var doubleIndent = indent + indent;
                    Debug.WriteLine("Found OptionsToStringAttribute nested");
                    formatParameters += ",formatMethod:(o) => { return Environment.NewLine + $\"{extraIndent}" + doubleIndent + "Count: {o.Count()}\" + Environment.NewLine + $\"{extraIndent}" + doubleIndent + "\"+ string.Join($\"{extraIndent}" + doubleIndent + "\", o.Select( oo => oo.Value.OptionsToString(\"" + doubleIndent + "\"+ extraIndent, $\"[{Mask.Quote(oo.Key)}]\") ?? \"null\"));},noQuote:true";
                }
                if (!ignored)
                    sb.AppendFormat(format, $"{nameQuote}{member.Name}{nameQuote}").Append(member.Name).Append(formatParameters).AppendLine($")}}{trailingComma}");
            }

            // end of method brace
            sb.Append($$"""
                      {{jsonClose}}";
                              }

                      """);
        }

        // end of class and namespace braces
        sb.Append(@"    }
}
");

        return sb.ToString();
    }

    private bool IsEnumerableOfOptionToString(IPropertySymbol member)
    {
        if (member.Type is IArrayTypeSymbol arrayTypeSymbol 
            && member.GetAttributes().Any(o => string.Equals(o.AttributeClass?.Name, nameof(OutputEnumerableAttribute))
            && string.Equals(o.AttributeClass?.ContainingNamespace?.ToString(), "Seekatar.OptionToStringGenerator")))
        {
            var listType = arrayTypeSymbol!.ElementType;
            return listType is not null
                && listType.TypeKind == TypeKind.Class
                && listType.SpecialType != SpecialType.System_String
                && listType.GetAttributes().Any(o => string.Equals(o.AttributeClass?.Name, nameof(OptionsToStringAttribute))
                                                                && string.Equals(o.AttributeClass?.ContainingNamespace?.ToString(), "Seekatar.OptionToStringGenerator"));
        }
        else if (member.Type is INamedTypeSymbol namedTypeSymbol 
                 && member.GetAttributes().Any(o => string.Equals(o.AttributeClass?.Name, nameof(OutputEnumerableAttribute))
                                                            && string.Equals(o.AttributeClass?.ContainingNamespace?.ToString(), "Seekatar.OptionToStringGenerator")))
        {
            var listType = namedTypeSymbol!.TypeArguments!.FirstOrDefault()!;
            return listType is not null
                && listType.TypeKind == TypeKind.Class
                && listType.SpecialType != SpecialType.System_String
                && listType.GetAttributes().Any(o => string.Equals(o.AttributeClass?.Name, nameof(OptionsToStringAttribute))
                                                                && string.Equals(o.AttributeClass?.ContainingNamespace?.ToString(), "Seekatar.OptionToStringGenerator"));
        }
        return false;
    }

    private bool IsDictionaryOfOptionToString(IPropertySymbol member)
    {   
        if (member.Type is INamedTypeSymbol namedTypeSymbol
                 && member.GetAttributes().Any(o => string.Equals(o.AttributeClass?.Name, nameof(OutputDictionaryAttribute))
                                                            && string.Equals(o.AttributeClass?.ContainingNamespace?.ToString(), "Seekatar.OptionToStringGenerator"))
                 && namedTypeSymbol!.TypeArguments!.Length == 2)
        {
            var dictionaryType = namedTypeSymbol!.TypeArguments[1];
            return dictionaryType is not null
                && dictionaryType.TypeKind == TypeKind.Class
                && dictionaryType.SpecialType != SpecialType.System_String
                && dictionaryType.GetAttributes().Any(o => string.Equals(o.AttributeClass?.Name, nameof(OptionsToStringAttribute))
                                                                && string.Equals(o.AttributeClass?.ContainingNamespace?.ToString(), "Seekatar.OptionToStringGenerator"));
        }
        return false;
    }

    // adapted from https://github.com/dotnet/extensions/blob/d58517b455f1182b555e5cc4ad48cb2936f0221b/src/Generators/Microsoft.Gen.Logging/Parsing/Parser.TagProvider.cs
    private IMethodSymbol? ValidateTagProvider(
            ITypeSymbol providerType,
            string? providerMethodName,
            ITypeSymbol tagCollectorType,
            ITypeSymbol complexObjType,
            Location? attrLocation,
            Compilation compilation,
            SourceProductionContext context)
    {
        if (providerType is IErrorTypeSymbol)
        {
            return null;
        }

        if (providerMethodName != null)
        {
            var methodSymbols = providerType.GetMembers(providerMethodName).Where(m => m.Kind == SymbolKind.Method).Cast<IMethodSymbol>();
            bool visitedLoop = false;
            foreach (var method in methodSymbols)
            {
                visitedLoop = true;
                ITypeSymbol? underlyingType = null;

                // parameter must be nullable since base object may be null
                // type of member may or may not be nullable
                if (method.IsStatic
                    && method.ReturnType.OriginalDefinition.SpecialType == SpecialType.System_String
                    && !method.IsGenericMethod
                    && IsParameterCountValid(method)
                    && method.Parameters[0].RefKind == RefKind.None)
                {
                    var a = SymbolEqualityComparer.Default.Equals(tagCollectorType, method.Parameters[0].Type);
                    var b = IsAssignableTo(complexObjType, method.Parameters[0].Type);
                    System.Diagnostics.Debug.WriteLine($"a: {a}, b: {b}");
                    System.Diagnostics.Debug.WriteLine($"Tag: {tagCollectorType}, Method: {method.Parameters[0].Type}");

                    underlyingType = method.Parameters[0].Type;

                    if (complexObjType is INamedTypeSymbol complexNamedTypeSymbol
                         && complexNamedTypeSymbol.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
                    {
                        if (method.Parameters[0].Type is INamedTypeSymbol namedTypeSymbol
                            && namedTypeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                        {
                            underlyingType = namedTypeSymbol.TypeArguments[0];
                            // Now underlyingType represents the underlying type of the nullable type.
                        }
                    }

                    var c = SymbolEqualityComparer.Default.Equals(tagCollectorType, underlyingType);
                    var d = IsAssignableTo(complexObjType, underlyingType);
                    System.Diagnostics.Debug.WriteLine($"c: {c}, d {d}");
                    System.Diagnostics.Debug.WriteLine($"Tag: {tagCollectorType}, Method: {underlyingType}");
                }


#pragma warning disable S1067 // Expressions should not be too complex
                if (method.IsStatic
                    && method.ReturnType.OriginalDefinition.SpecialType == SpecialType.System_String
                    && !method.IsGenericMethod
                    && IsParameterCountValid(method)
                    && method.Parameters[0].RefKind == RefKind.None
                    && underlyingType is not null
                    && SymbolEqualityComparer.Default.Equals(tagCollectorType, underlyingType)
                    && IsAssignableTo(complexObjType, underlyingType))
#pragma warning restore S1067 // Expressions should not be too complex
                {
                    if (IsProviderMethodVisible(method))
                    {
                        return method;
                    }

                    context.Report(SEEK010, attrLocation,
                        providerType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                        providerMethodName, complexObjType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                    return null;
                }
            }

            if (visitedLoop)
            {
                context.Report(SEEK010, attrLocation,
                    providerType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    providerMethodName, complexObjType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                return null;
            }
        }

        context.Report(SEEK009, attrLocation, providerMethodName ?? "", providerType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
        return null;

        static bool IsParameterCountValid(IMethodSymbol method) => method.Parameters.Length == 1;

        bool IsAssignableTo(ITypeSymbol type, ITypeSymbol target)
        {
            if (type.NullableAnnotation == NullableAnnotation.Annotated)
            {
                if (target.NullableAnnotation == NullableAnnotation.NotAnnotated)
                {
                    return false;
                }
            }

            if (target.TypeKind == TypeKind.Interface)
            {
                if (SymbolEqualityComparer.Default.Equals(type.WithNullableAnnotation(NullableAnnotation.None), target.WithNullableAnnotation(NullableAnnotation.None)))
                {
                    return true;
                }

                foreach (var iface in type.AllInterfaces)
                {
                    if (SymbolEqualityComparer.Default.Equals(target.WithNullableAnnotation(NullableAnnotation.None), iface.WithNullableAnnotation(NullableAnnotation.None)))
                    {
                        return true;
                    }
                }

                return false;
            }

            return IsBaseOrIdentity(type, target, compilation);
        }

        static bool IsBaseOrIdentity(ITypeSymbol source, ITypeSymbol dest, Compilation comp)
        {
            var conversion = comp.ClassifyConversion(source, dest);
            return conversion.IsIdentity || (conversion.IsReference && conversion.IsImplicit);
        }

        static bool IsProviderMethodVisible(ISymbol symbol)
        {
            while (symbol != null && symbol.Kind != SymbolKind.Namespace)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.NotApplicable:
                    case Accessibility.Private:
                    case Accessibility.Protected:
                    return false;
                }

                symbol = symbol.ContainingSymbol;
            }

            return true;
        }
    }
}
