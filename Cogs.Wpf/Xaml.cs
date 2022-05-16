namespace Cogs.Wpf;

/// <summary>
/// Provides general XAML utilties
/// </summary>
public static class Xaml
{
    /// <summary>
    /// Parses the specified XAML using the specified namespaces and mapped types, returning the constructed XAML element
    /// </summary>
    /// <typeparam name="T">The type of the constructed element</typeparam>
    /// <param name="xaml">The XAML</param>
    /// <param name="namespaces">The namespaces</param>
    /// <param name="mappedTypes">The mapped types</param>
    public static T Parse<T>(string xaml, IReadOnlyDictionary<string, string>? namespaces = null, IReadOnlyDictionary<string, Type>? mappedTypes = null)
    {
        namespaces ??= new Dictionary<string, string>
        {
            { string.Empty, "http://schemas.microsoft.com/winfx/2006/xaml/presentation" },
            { "x", "http://schemas.microsoft.com/winfx/2006/xaml" }
        };
        var xamlTypeMapper = new XamlTypeMapper(Array.Empty<string>());
        if (mappedTypes is not null)
        {
            namespaces = namespaces
                .Concat(mappedTypes.Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Key)))
                .ToImmutableDictionary();
            foreach (var type in mappedTypes)
                xamlTypeMapper.AddMappingProcessingInstruction(type.Key, type.Value.Namespace, type.Value.Assembly.FullName);
        }
        var parserContext = new ParserContext { XamlTypeMapper = xamlTypeMapper };
        foreach (var @namespace in namespaces)
            parserContext.XmlnsDictionary.Add(@namespace.Key, @namespace.Value);
        return (T)XamlReader.Parse(xaml, parserContext);
    }
}
