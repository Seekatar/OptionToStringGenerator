using Microsoft.CodeAnalysis;

namespace Seekatar.OptionToStringGenerator;

public static class DiagnosticTemplates
{
    public enum Ids
    {
        SEEK001,
        SEEK002,
        SEEK003,
        SEEK004,
        SEEK005,
        SEEK006,
        SEEK007,
    }
    static List<DiagnosticDescriptor> _diagnostics = new() {
        new DiagnosticDescriptor(
                id: Ids.SEEK001.ToString(),
                title: "Missing or invalid regex parameter",
                messageFormat: "Bad Regex: {0}",
                category: "Usage",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek001-missing-or-invalid-regex-parameter"
                ),
         new DiagnosticDescriptor(
                id: Ids.SEEK002.ToString(),
                title: "Multiple format attributes",
                messageFormat: "You can only use one formatting attribute on a property",
                category: "Usage",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek002-multiple-format-attributes"
                ),
        new DiagnosticDescriptor(
                id: Ids.SEEK003.ToString(),
                title: "No properties found",
                messageFormat: "No public properties have an Output* attribute",
                category: "Usage",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek003-no-properties-found"
                ),
        new DiagnosticDescriptor(
                id: Ids.SEEK004.ToString(),
                title: "Member in Title not found",
                messageFormat: "Property '{0}' not found on {1}",
                category: "Usage",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek004-member-in-title-not-found"
                ),
        new DiagnosticDescriptor(
                id: Ids.SEEK005.ToString(),
                title: "Private classes can't be used",
                messageFormat: "The class '{0}' is private",
                category: "Usage",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek005-private-classes-cant-be-used"
                ),
        new DiagnosticDescriptor(
                id: Ids.SEEK006.ToString(),
                title: "Member not found on class",
                messageFormat: "The member '{0}' in the attribute isn't in the class '{1}'",
                category: "Usage",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek005-private-classes-cant-be-used"
                ),
        new DiagnosticDescriptor(
                id: Ids.SEEK007.ToString(),
                title: "Name is required",
                messageFormat: "The attribute '{0}' didn't have a Name set",
                category: "Usage",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek005-private-classes-cant-be-used"
                )
    };

    public static void Report(this SourceProductionContext context, Ids id, Location? location, params string[] args)
    {
        var d = _diagnostics.FirstOrDefault(o => id.ToString() == o.Id);
        if (d is null) throw new Exception($"Unknown diagnostic id {id}");

        context.ReportDiagnostic(Diagnostic.Create(d, location, args));
    }
}
