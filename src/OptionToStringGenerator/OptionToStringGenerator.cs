using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

using static Seekatar.OptionToStringGenerator.DiagnosticTemplates.Ids;
namespace Seekatar.OptionToStringGenerator;

public class ClassToGenerate : ItemToGenerate
{
    public readonly List<IPropertySymbol> Values;

    public INamedTypeSymbol ClassSymbol {  get; }

    public ClassToGenerate(INamedTypeSymbol classSymbol, List<IPropertySymbol> values) : base(classSymbol)
    {
        ClassSymbol = classSymbol;
        Values = values;
    }
}

[Generator]
public class OptionToStringGenerator : OptionGeneratorBase<ClassDeclarationSyntax,ClassToGenerate>
{
    public const string FullAttributeName = "Seekatar.OptionToStringGenerator.OptionsToStringAttribute";

    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Do a simple filter for classes
        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),        // get classes with an attribute, must be fast
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)) // get a class with the my attribute
            .Where(static m => m is not null)!; // filter out attributed classes that we don't care about

        // Combine the selected classes with the `Compilation`
        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses
            = context.CompilationProvider.Combine(classDeclarations.Collect());

        // Generate the source using the compilation and classes
        context.RegisterSourceOutput(compilationAndClasses,
            (spc, source) => Execute("ClassExtensions", source.Item1, source.Item2, spc));
    }
    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    => node is ClassDeclarationSyntax m && m.AttributeLists.Count > 0;

    static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        // we know the node is a ClassDeclarationSyntax thanks to IsSyntaxTargetForGeneration
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
		
        return HasAttribute(context, FullAttributeName, classDeclarationSyntax.AttributeLists) ? classDeclarationSyntax : null;

    }

    protected override List<ClassToGenerate> GetTypesToGenerate(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classSyntax, CancellationToken ct, SourceProductionContext context)
    {
        var classToGenerate = new List<ClassToGenerate>();

        // Get the semantic representation of our marker attribute
        INamedTypeSymbol? classAttribute = compilation.GetTypeByMetadataName(FullAttributeName);

        if (classAttribute == null)
        {
            // If this is null, the compilation couldn't find the marker attribute type
            // which suggests there's something very wrong! Bail out..
            return classToGenerate;
        }

        foreach (ClassDeclarationSyntax classDeclarationSyntax in classSyntax)
        {
            // stop if we're asked to
            ct.ThrowIfCancellationRequested();

            // Get the semantic representation of the class syntax
            SemanticModel semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
            {
                // something went wrong, bail out
                continue;
            }

            // Get all the members in the class
            ImmutableArray<ISymbol> classMembers = classSymbol.GetMembers();
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

            // Create an ClassToGenerate for use in the generation phase
            classToGenerate.Add(new ClassToGenerate(classSymbol, members));
        }

        return classToGenerate;
    }

    protected override string GenerateExtensionClass(List<ClassToGenerate> classesToGenerate, SourceProductionContext context)
    {
        var sb = new StringBuilder();
        sb.Append("""
                    #nullable enable
                    namespace Seekatar.OptionToStringGenerator
                    {
                        public static partial class ClassExtensions
                        {

                    """);
        foreach (var classToGenerate in classesToGenerate)
        {
            if (classToGenerate.Accessibility == "private") 
            {
                context.Report(SEEK005, classToGenerate.Location, classToGenerate.Name);
                continue; 
            }

            var className = classToGenerate.Name;
            var classAccessibility = classToGenerate.Accessibility;
            var members = classToGenerate.Values;
            var formatAttribute = classToGenerate.ClassSymbol.GetAttributes().Where(a => a.AttributeClass?.ContainingNamespace?.ToString() == "Seekatar.OptionToStringGenerator").FirstOrDefault();
            if (formatAttribute is null) { continue; } // can't get here if it doesn't have the attribute, but just in case

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
                        // loop over all the  regex matches, and see if the string is a member of the class
                        var titleString = n.Value.Value.ToString();
                        var matches = regex.Matches(titleString);
                        foreach (Match match in matches)
                        {
                            var memberName = match.Groups[1].Value;
                            var member = members.Where(m => m.Name == memberName).FirstOrDefault();
                            if (member is null)
                            {
                                context.Report(SEEK004, classToGenerate.Location, memberName, classToGenerate.Name);
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
                context.Report(SEEK003, classToGenerate.Location);
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
                ImmutableArray<AttributeData> attributes = AttributesForMember(member, classToGenerate); 
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

    protected override ImmutableArray<AttributeData> AttributesForMember(IPropertySymbol symbol, ClassToGenerate propertyToGenerate)
    {
        return symbol.GetAttributes();
    }
}
