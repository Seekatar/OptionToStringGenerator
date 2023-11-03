using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Schema;

namespace Seekatar.OptionToStringGenerator;

public readonly struct PropertyToGenerate
{
    public string Name => PropertySymbol.ToString();
    public Location Location => PropertySymbol.Locations[0];
    public readonly IPropertySymbol PropertySymbol { get; }
    public string Accessibility => PropertySymbol.DeclaredAccessibility.ToString().ToLowerInvariant();
    public Dictionary<string, AttributeData> Attributes { get; }
    public PropertyToGenerate(IPropertySymbol propertySymbol, Dictionary<string, AttributeData> attrs )
    {
        PropertySymbol = propertySymbol;
        Attributes = attrs;
    }
}

[Generator]
public class OptionPropertyToStringGenerator : IIncrementalGenerator
{
    public const string FullAttributeName = "Seekatar.OptionToStringGenerator.OutputPropertyMaskAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Do a simple filter for properties
        IncrementalValuesProvider<PropertyDeclarationSyntax> propertyDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),        // get properties with an attribute, must be fast
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)) // get a property with the my attribute
            .Where(static m => m is not null)!; // filter out attributed properties that we don't care about

        // Combine the selected properties with the `Compilation`
        IncrementalValueProvider<(Compilation, ImmutableArray<PropertyDeclarationSyntax>)> compilationAndProperties
            = context.CompilationProvider.Combine(propertyDeclarations.Collect());

        // Generate the source using the compilation and properties
        context.RegisterSourceOutput(compilationAndProperties,
            static (spc, source) => Execute(source.Item1, source.Item2, spc));
    }
    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    => node is PropertyDeclarationSyntax m && m.AttributeLists.Count > 0;

    static PropertyDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        // we know the node is a PropertyDeclarationSyntax thanks to IsSyntaxTargetForGeneration
        var propertyDeclarationSyntax = (PropertyDeclarationSyntax)context.Node;

        // attribute list
        // IdentifierNameSyntax has Identifier Token
        // loop through all the attributes
        foreach (AttributeListSyntax attributeListSyntax in propertyDeclarationSyntax.AttributeLists)
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
                if (fullName == FullAttributeName)
                {
                    // return the property
                    return propertyDeclarationSyntax;
                }
            }
        }

        // we didn't find the attribute we were looking for
        return null;
    }

    static void Execute(Compilation compilation, ImmutableArray<PropertyDeclarationSyntax> properties, SourceProductionContext context)
    {
        if (properties.IsDefaultOrEmpty)
        {
            // nothing to do yet
            return;
        }

        // I'm not sure if this is actually necessary, but `[LoggerMessage]` does it, so seems like a good idea!
        IEnumerable<PropertyDeclarationSyntax> distinctProperties = properties.Distinct();

        // Convert each PropertyDeclarationSyntax to an PropertyToGenerate
        List<PropertyToGenerate> propertiesToGenerate = GetTypesToGenerate(compilation, distinctProperties, context.CancellationToken, context);

        // If there were errors in the PropertyDeclarationSyntax, we won't create an
        // PropertyToGenerate for it, so make sure we have something to generate
        if (propertiesToGenerate.Count > 0)
        {
            // generate the source code and add it to the output
            string result = GenerateExtensionClass(propertiesToGenerate, context);
            context.AddSource("PropertyExtensions.g.cs", SourceText.From(result, Encoding.UTF8));
        }
    }

    static List<PropertyToGenerate> GetTypesToGenerate(Compilation compilation, IEnumerable<PropertyDeclarationSyntax> propertySyntax, CancellationToken ct, SourceProductionContext context)
    {
        var propertyToGenerate = new List<PropertyToGenerate>();

        // Get the semantic representation of our marker attribute
        INamedTypeSymbol? propertyAttribute = compilation.GetTypeByMetadataName(FullAttributeName);

        if (propertyAttribute == null)
        {
            // If this is null, the compilation couldn't find the marker attribute type
            // which suggests there's something very wrong! Bail out..
            return propertyToGenerate;
        }

        foreach (PropertyDeclarationSyntax propertyDeclarationSyntax in propertySyntax)
        {
            // stop if we're asked to
            ct.ThrowIfCancellationRequested();

            // Get the semantic representation of the property syntax
            SemanticModel semanticModel = compilation.GetSemanticModel(propertyDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(propertyDeclarationSyntax) is not IPropertySymbol propertySymbol)
            {
                // something went wrong, bail out
                continue;
            }
            var propertyType = propertySymbol.Type;
            if (propertyType is not INamedTypeSymbol typeSymbol)
            {
                continue;
            }
            //var attrs = propertyType.GetAttributes(); // we should have attributes by now?
            //var t = typeSymbol.MemberNames;
            //var m = typeSymbol.GetMembers();
            //var m2 = typeSymbol.GetMembers("Name");
            //var m3 = (m2!.ElementAt(0)! as IPropertySymbol)!.GetMethod;

            // get all the attributes on the property
            var attrs = propertySymbol.GetAttributes().Where(a => a.AttributeClass?.ContainingNamespace?.ToString() == "Seekatar.OptionToStringGenerator");
            attrs = attrs.Where(a => a.AttributeClass?.Name.StartsWith("OutputProperty") ?? false);
            var attrDict = new Dictionary<string, AttributeData>();
            foreach ( var a in attrs)
            {
                var name = a.NamedArguments.FirstOrDefault(o => o.Key == nameof(IPropertyAttribute.Name)).Value.Value?.ToString();
                if (name is null)
                {
                    var diag = Diagnostic.Create(new DiagnosticDescriptor(
                                            id: "SEEK006",
                                            title: "Name is required",
                                            messageFormat: $"The attribute '{a.ToString()}' didn't have a Name set",
                                            category: "Usage",
                                            defaultSeverity: DiagnosticSeverity.Warning,
                                            isEnabledByDefault: true,
                                            helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek005-private-properties-cant-be-used"
                                         ), a.AttributeClass?.Locations[0]);
                    context.ReportDiagnostic(diag);

                }
                else
                {
                    attrDict.Add(name, a);
                }
            }

            propertyToGenerate.Add(new PropertyToGenerate(propertySymbol, attrDict));
        }

        return propertyToGenerate;
    }

    public static string GenerateExtensionClass(List<PropertyToGenerate> propertiesToGenerate, SourceProductionContext context)
    {
        var sb = new StringBuilder();
        sb.Append("""
                    #nullable enable
                    namespace Seekatar.OptionToStringGenerator
                    {
                        public static partial class PropertyExtensions
                        {

                    """);
        foreach (var propertyToGenerate in propertiesToGenerate)
        {
            AttributeData? propertyAttribute = null!;
            AttributeData? propertyAttributeForClass = null; // TODO

            if (propertyToGenerate.Accessibility == "private")
            {
                var diag = Diagnostic.Create(new DiagnosticDescriptor(
                                        id: "SEEK005",
                                        title: "Private properties can't be used",
                                        messageFormat: $"The property '{propertyToGenerate.Name}' is private",
                                        category: "Usage",
                                        defaultSeverity: DiagnosticSeverity.Warning,
                                        isEnabledByDefault: true,
                                        helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek005-private-properties-cant-be-used"
                                     ), propertyToGenerate.Location);
                context.ReportDiagnostic(diag);
                continue;
            }

            // get the type of the property and its members
            var propertyType = propertyToGenerate.PropertySymbol.Type; 
            if (propertyType is not INamedTypeSymbol typeSymbol)
            {
                continue;
            }
            if (propertyType.TypeKind is not TypeKind.Class or TypeKind.Interface)
                continue; // int is struct

            // var members = typeSymbol.GetMembers();
            //var m2 = typeSymbol.GetMembers("Name");
            //var m3 = (m2!.ElementAt(0)! as IPropertySymbol)!.GetMethod;
            ImmutableArray<ISymbol> classMembers = typeSymbol.GetMembers();
            var members = new List<IPropertySymbol>(classMembers.Length);

            // Get all the public properties with a get method
            foreach (ISymbol member in classMembers)
            {
                if (member is IPropertySymbol property
                    && property.GetMethod is not null
                    && property.DeclaredAccessibility == Accessibility.Public)
                {
                    members.Add(property);
                }
            }
            
            int maxLen = 0;
            foreach (var member in members)
            {
                if (!propertyToGenerate.Attributes.TryGetValue(member.Name, out propertyAttribute))
                    continue; // TODO warning
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
            var title = propertyToGenerate.Name;
            var titleText = "";

            if (propertyAttributeForClass is not null)
            {
                foreach (var n in propertyAttributeForClass.NamedArguments)
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
                                var diag = Diagnostic.Create(new DiagnosticDescriptor(
                                                        id: "SEEK004",
                                                        title: "Member in Title not found",
                                                        messageFormat: $"Property '{memberName}' not found on {propertyToGenerate.Name}",
                                                        category: "Usage",
                                                        defaultSeverity: DiagnosticSeverity.Warning,
                                                        isEnabledByDefault: true,
                                                        helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek004-member-in-title-not-found"
                                                     ), propertyToGenerate.Location);
                                context.ReportDiagnostic(diag);
                                titleString = titleString.Replace($"{{{memberName}}}", memberName);
                            }
                            else
                            {
                                titleString = titleString.Replace($"{{{memberName}}}", $"{{o.{memberName}}}");
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

            if (haveJson) {
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
            sb.Append($"        {propertyToGenerate.Accessibility} static string OptionsToString(this ").Append(propertyToGenerate.Name).Append(
                      $$""""
                       o)
                              {
                                  return $@"
                      """");

            sb.Append($"{titleText}").AppendLine();

            if (!members.Any())
            {
                var diag = Diagnostic.Create(new DiagnosticDescriptor(
                                        id: "SEEK003",
                                        title: "No properties found",
                                        messageFormat: "No public properties have an Output* attribute",
                                        category: "Usage",
                                        defaultSeverity: DiagnosticSeverity.Warning,
                                        isEnabledByDefault: true,
                                        helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek003-no-properties-found"
                                     ), propertyToGenerate.Location );
                context.ReportDiagnostic(diag);
                sb.AppendLine($"{indent}No properties to display");
            }

            // each property
            string format = $"{indent}{{0,-{maxLen}}} {separator} {{{{OptionsToStringAttribute.Format(o.";
            int j = 0;
            foreach (var member in members)
            {
                if (++j == members.Count)
                    trailingComma = "";

                bool ignored = false;
                ImmutableArray<AttributeData> attributes = AttributesForMember(member.Name, propertyToGenerate);
                var formatParameters = haveJson ? $",asJson:{haveJson.ToString().ToLowerInvariant()}" : "";
                var attributeCount = 0;
                for (int i = 0; i < attributes.Length; i++)
                {
                    var attribute = attributes[i];
                    if (attribute.AttributeClass?.ContainingNamespace?.ToString() == "Seekatar.OptionToStringGenerator")
                    {
                        attributeCount++;
                        if (attribute.AttributeClass?.Name.EndsWith("IgnoreAttribute") ?? false)
                            ignored = true;
                        else if (attribute.AttributeClass?.Name.EndsWith("MaskAttribute") ?? false)
                        {
                            var prefixLen = "0";
                            var suffixLen = "0";
                            for ( int k = 0; k < attribute.NamedArguments.Length; k++)
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
                                        message = "Bad regex: " + e.Message;
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
                                var diag = Diagnostic.Create(new DiagnosticDescriptor(
                                                        id: "SEEK001",
                                                        title: "Missing or invalid regex parameter",
                                                        messageFormat: message,
                                                        category: "Usage",
                                                        defaultSeverity: DiagnosticSeverity.Error,
                                                        isEnabledByDefault: true,
                                                        helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek001-missing-or-invalid-regex-parameter"
                                                     ), member.Locations[0]);
                                context.ReportDiagnostic(diag);
                            }
                        }
                    }
                }

                if (attributeCount > 1)
                {
                    var diag = Diagnostic.Create(new DiagnosticDescriptor(
                        id: "SEEK002",
                        title: "Multiple format attributes",
                        messageFormat: "You can only use one formatting attribute on a property",
                        category: "Usage",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek002-multiple-format-attributes"
                        ), member.Locations[0]);
                    context.ReportDiagnostic(diag);
                }
                if (!ignored)
                    sb.AppendFormat(format, $"{nameQuote}{member.Name}{nameQuote}").Append(member.Name).Append(formatParameters).AppendLine($")}}{trailingComma}");
            }

            // end of method
            sb.Append($$"""
                      {{jsonClose}}";
                              }
                      """);
        }

        sb.Append(@"
    }
}");

        return sb.ToString();
    }

    private static ImmutableArray<AttributeData> AttributesForMember(string name, PropertyToGenerate propertyToGenerate)
    {
        return propertyToGenerate.Attributes.Where(a => a.Key == name).Select(a => a.Value).ToImmutableArray();
    }
}
