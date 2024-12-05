using Seekatar.OptionToStringGenerator;
using System.Collections.Generic;

namespace Test;

[OptionsToString]
public class ArrayAndDictionaryOfOptions
{
    [OptionsToString]
    public class NestedItem
    {
        public string Name { get; set; } = "";
    }

    public IEnumerable<NestedItem> List { get; set; } = new List<NestedItem>
            { 
                new() { Name = "Name1" },
                new() { Name = "Name2" }
            };

    public IDictionary<string, NestedItem> Dictionary { get; set; } = new Dictionary<string, NestedItem>
        {
                { "A", new () { Name = "NameA" } },
                { "B", new() { Name = "NameB" } }
        };
}
