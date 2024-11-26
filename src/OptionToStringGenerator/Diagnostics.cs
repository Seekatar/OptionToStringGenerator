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
        SEEK008,
        SEEK009,
        SEEK010
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
                messageFormat: "Multiple format attributes found. Using first one",
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
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek006-member-not-found-in-class"
                ),
        new DiagnosticDescriptor(
                id: Ids.SEEK007.ToString(),
                title: "Name is required",
                messageFormat: "The attribute '{0}' has an empty Name",
                category: "Usage",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek007-name-is-required"
                ),
        new DiagnosticDescriptor(
                id: Ids.SEEK008.ToString(),
                title: "Invalid Type for Property",
                messageFormat: "The Property '{0}' of type '{1}' is type '{2}'. It must be class, record, or interface",
                category: "Usage",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek008-invalid-type-for-property"
                ),
        new DiagnosticDescriptor(
                id: Ids.SEEK009.ToString(),
                title: "Missing method for provider",
                messageFormat: "Could not find the method '{0}' on type {1}",
                category: "Usage",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek009-missing-method-for-provider"
                ),
        new DiagnosticDescriptor(
                id: Ids.SEEK010.ToString(),
                title: "Invalid provider signature",
                messageFormat: "The signature of '{0}.{1}' should be static string? {1}({2})",
                category: "Usage",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                helpLinkUri: "https://github.com/Seekatar/OptionToStringGenerator/wiki/Error-Messages#seek010-invalid-provider-signature"
                )
    };

    public static void Report(this SourceProductionContext context, Ids id, Location? location, params string[] args)
    {
        var d = _diagnostics.FirstOrDefault(o => id.ToString() == o.Id) ?? throw new Exception($"Unknown diagnostic id {id}");
        context.ReportDiagnostic(Diagnostic.Create(d, location, args));
    }
}
