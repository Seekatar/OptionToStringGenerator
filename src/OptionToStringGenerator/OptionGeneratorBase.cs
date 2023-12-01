using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
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
            string result = GenerateExtensionClass(propertiesToGenerate, context);
            context.AddSource(sourceName+".g.cs", SourceText.From(result, Encoding.UTF8));
        }
    }
    protected abstract List<TGeneratedItem> GetTypesToGenerate(Compilation compilation, IEnumerable<TSyntax> distinctProperties, CancellationToken token, SourceProductionContext context);

    protected abstract ImmutableArray<AttributeData> AttributesForMember(IPropertySymbol symbol, TGeneratedItem propertyToGenerate);

    protected List<IPropertySymbol> GetAllPublicProperties(INamedTypeSymbol classSymbol, bool? excludeParent, List<IPropertySymbol>? members = null)
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
            GetAllPublicProperties(baseType, false, members);
        }
        return members;
    }


    protected string GenerateExtensionClass(List<TGeneratedItem> itemsToGenerate, SourceProductionContext context)
    {
        var sb = new StringBuilder();
        sb.Append("""
                    #nullable enable
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
                titleText = $"{title}{nameSuffix}";
            }

            // method signature
            sb.Append($"        {classAccessibility} static string OptionsToString(this ").Append(className).Append(
                      $$""""
                       o)
                              {
                                  return $@"
                      """");

            sb.Append($"{titleText}").AppendLine();

            if (!members.Any())
            {
                context.Report(SEEK003, itemToGenerate.Location);
                sb.AppendLine($"{indent}No properties to display");
            }

            // each property
            string format = $"{indent}{{0,-{maxLen}}} {separator} {{{{OptionsToStringAttribute.Format(o?.";
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
                    }
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

}
