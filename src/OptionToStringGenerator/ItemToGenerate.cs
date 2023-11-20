using Microsoft.CodeAnalysis;
namespace Seekatar.OptionToStringGenerator;

public abstract class ItemToGenerate
{
    public string Name { get; protected set; } 
    public Location Location => Symbol.Locations[0];
    public ISymbol Symbol { get; }
    public string Accessibility { get; protected set; } 

    public readonly List<IPropertySymbol> Values;
    public abstract AttributeData? GetFormat();

    public ItemToGenerate(ISymbol symbol, List<IPropertySymbol> values)
    {
        Values = values;
        Symbol = symbol;
        Name = Symbol.ToString();
        Accessibility = Symbol.DeclaredAccessibility.ToString().ToLowerInvariant();
    }
}
