using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

using static Seekatar.OptionToStringGenerator.DiagnosticTemplates.Ids;
namespace Seekatar.OptionToStringGenerator;

public class ItemToGenerate
{
    public string Name => Symbol.ToString();
    public Location Location => Symbol.Locations[0];
    public ISymbol Symbol { get; }
    public string Accessibility => Symbol.DeclaredAccessibility.ToString().ToLowerInvariant();

    public ItemToGenerate(ISymbol propertySymbol)
    {
        Symbol = propertySymbol;
    }
}


public class PropertyToGenerate :  ItemToGenerate
{
    public Dictionary<string, AttributeData> Attributes { get; }
    public IPropertySymbol PropertySymbol { get; }
    public PropertyToGenerate(IPropertySymbol propertySymbol, Dictionary<string, AttributeData> attrs ) : base(propertySymbol)
    {
        PropertySymbol = propertySymbol;
        Attributes = attrs;
    }
}

public abstract class OptionGeneratorBase<TSyntax,TGeneratedItem> : IIncrementalGenerator where TSyntax : MemberDeclarationSyntax
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
    protected abstract string GenerateExtensionClass(List<TGeneratedItem> propertiesToGenerate, SourceProductionContext context);
    protected abstract ImmutableArray<AttributeData> AttributesForMember(IPropertySymbol symbol, TGeneratedItem propertyToGenerate);
}

[Generator]
public class OptionPropertyToStringGenerator : OptionGeneratorBase<PropertyDeclarationSyntax, PropertyToGenerate> 
{
    public const string FullAttributeName = "Seekatar.OptionToStringGenerator.OutputPropertyMaskAttribute";

    public override void Initialize(IncrementalGeneratorInitializationContext context)
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
            (spc, source) => Execute("PropertyExtensions", source.Item1, source.Item2, spc));
    }
    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    => node is PropertyDeclarationSyntax m && m.AttributeLists.Count > 0;

    static PropertyDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        // we know the node is a PropertyDeclarationSyntax thanks to IsSyntaxTargetForGeneration
        var propertyDeclarationSyntax = (PropertyDeclarationSyntax)context.Node;

        return HasAttribute(context, FullAttributeName, propertyDeclarationSyntax.AttributeLists) ? propertyDeclarationSyntax : null;
    }

    protected override List<PropertyToGenerate> GetTypesToGenerate(Compilation compilation, IEnumerable<PropertyDeclarationSyntax> propertySyntax, CancellationToken ct, SourceProductionContext context)
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
                                            messageFormat: $"The attribute '{a}' didn't have a Name set",
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

    protected override string GenerateExtensionClass(List<PropertyToGenerate> propertiesToGenerate, SourceProductionContext context)
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
            AttributeData? propertyAttribute = null;
            AttributeData? formatAttribute = propertyToGenerate.Attributes.FirstOrDefault(a => a.Value.AttributeClass?.Name == nameof(OutputPropertyFormatAttribute)).Value;

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

            var className = typeSymbol.ContainingNamespace+"."+typeSymbol.Name;
            var classAccessibility = typeSymbol.DeclaredAccessibility.ToString().ToLowerInvariant();

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
                                context.Report(SEEK004, propertyToGenerate.Location, memberName, propertyToGenerate.Name);
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
            sb.Append($"        {classAccessibility} static string OptionsToString(this ").Append(className).Append(
                      $$""""
                       o)
                              {
                                  return $@"
                      """");

            sb.Append($"{titleText}").AppendLine();

            if (!members.Any())
            {
                context.Report(SEEK003, propertyToGenerate.Location);
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
                ImmutableArray<AttributeData> attributes = AttributesForMember(member, propertyToGenerate);
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
                                context.Report(SEEK001, member.Locations[0], message);
                            }
                        }
                    }
                }

                if (attributeCount > 1)
                {
                    context.Report(SEEK002, member.Locations[0]);
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

    protected override ImmutableArray<AttributeData> AttributesForMember(IPropertySymbol symbol, PropertyToGenerate propertyToGenerate)
    {
        return propertyToGenerate.Attributes.Where(a => a.Key == symbol.Name).Select(a => a.Value).ToImmutableArray();
    }
}
