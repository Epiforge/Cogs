namespace Cogs.Exceptions;

/// <summary>
/// Provides extensions for dealing with exceptions
/// </summary>
public static class ExceptionExtensions
{
    static readonly ConcurrentDictionary<Type, IReadOnlyList<(string name, FastMethodInfo getter)>> fastPropertyGetters = new ConcurrentDictionary<Type, IReadOnlyList<(string name, FastMethodInfo getter)>>();
    static readonly HashSet<Type> getExceptionDetailsIgnoredAdditionalPropertyDeclaringTypes = new HashSet<Type>(new Type[] { typeof(Exception), typeof(ReflectionTypeLoadException), typeof(AggregateException) });
    static readonly HashSet<string> getExceptionDetailsIgnoredAdditionalPropertyNames = new HashSet<string>(new string[] { nameof(Exception.Data), nameof(Exception.HelpLink), nameof(Exception.Message), nameof(Exception.Source), nameof(Exception.StackTrace) });

    static IReadOnlyList<(string name, FastMethodInfo getter)> CreateFastPropertyGetters(Type type) =>
        type.GetRuntimeProperties()
            .Where(p => !getExceptionDetailsIgnoredAdditionalPropertyDeclaringTypes.Contains(p.DeclaringType) && !getExceptionDetailsIgnoredAdditionalPropertyNames.Contains(p.Name) && p.GetMethod is MethodInfo getMethod && getMethod.IsPublic && !getMethod.IsStatic)
            .Select(p => (name: $"{type.FullName}.{p.Name}", getter: FastMethodInfo.Get(p.GetMethod)))
            .ToImmutableList();

    static IEnumerable<(string name, object? value)> GetAdditionalProperties(Exception ex, Type type) =>
        fastPropertyGetters.GetOrAdd(type, CreateFastPropertyGetters).Select(nameAndGetter => (nameAndGetter.name, value: nameAndGetter.getter.Invoke(ex)));

    /// <summary>
    /// Creates a representation of an exception and all of its inner exceptions, including exception types, messages, and stack traces, and traversing multiple inner exceptions in the case of <see cref="AggregateException"/>
    /// </summary>
    /// <param name="ex">The exception for which to generate the representation</param>
    /// <param name="format">The format in which to create the representation</param>
    public static string GetFullDetails(this Exception ex, ExceptionFullDetailsFormat? format = null)
    {
        if (ex is null)
            throw new ArgumentNullException(nameof(ex));
        try
        {
            ex = ex.Demystify();
        }
        catch
        {
            // there's nothing to do if this fails
        }
        switch (format ?? DefaultExceptionFullDetailsFormat)
        {
            case ExceptionFullDetailsFormat.Json:
                using (var str = new StringWriter())
                {
                    using (var json = new JsonTextWriter(str))
                    {
                        json.Formatting = Newtonsoft.Json.Formatting.Indented;
                        GetFullDetailsInJson(ex, json);
                    }
                    return str.ToString();
                }
            case ExceptionFullDetailsFormat.Xml:
                using (var str = new StringWriter())
                {
                    using (var xml = XmlWriter.Create(str, new XmlWriterSettings
                    {
                        Indent = true,
                        NewLineOnAttributes = true,
                        OmitXmlDeclaration = true
                    }))
                    {
                        xml.WriteStartDocument();
                        xml.WriteStartElement("exception");
                        GetFullDetailsInXml(ex ?? throw new ArgumentNullException(nameof(ex)), xml);
                        xml.WriteEndDocument();
                    }
                    return str.ToString();
                }
            default:
                return GetFullDetailsInPlainText(ex, 0);
        }
    }

    static void GetFullDetailsInJson(Exception ex, JsonWriter json)
    {
        json.WriteStartObject();
        var type = ex.GetType();
        json.WritePropertyName("type");
        json.WriteValue(type.FullName);
        json.WritePropertyName("message");
        json.WriteValue(ex.Message);
        if (ex.Data?.Count > 0)
        {
            json.WritePropertyName("data");
            json.WriteStartArray();
            foreach (var key in ex.Data.Keys)
            {
                json.WriteStartObject();
                json.WritePropertyName("key");
                json.WriteValue(key.ToString());
                json.WritePropertyName("value");
                json.WriteValue(ex.Data[key].ToString());
                json.WriteEndObject();
            }
            json.WriteEndArray();
        }
        var additionalProperties = GetAdditionalProperties(ex, type);
        if (additionalProperties.Any())
        {
            json.WritePropertyName("properties");
            json.WriteStartObject();
            foreach (var (name, value) in additionalProperties)
            {
                json.WritePropertyName(name);
                if (value is { })
                    json.WriteValue(value.ToString());
                else
                    json.WriteNull();
            }
            json.WriteEndObject();
        }
        var stackTrace = new StackTrace(ex, true);
        json.WritePropertyName("stackTrace");
        json.WriteStartArray();
        foreach (var frame in stackTrace.GetFrames())
        {
            json.WriteStartObject();
            var method = frame.GetMethod();
            json.WritePropertyName("type");
            json.WriteValue(method.DeclaringType.FullName);
            json.WritePropertyName("method");
            json.WriteValue(GetMethodDescription(method));
            if (frame.GetFileName() is string fileName)
            {
                json.WritePropertyName("file");
                json.WriteValue(fileName);
                json.WritePropertyName("line");
                json.WriteValue(frame.GetFileLineNumber());
                json.WritePropertyName("column");
                json.WriteValue(frame.GetFileColumnNumber());
            }
            json.WritePropertyName("offset");
            json.WriteValue(frame.GetILOffset());
            json.WriteEndObject();
        }
        json.WriteEndArray();
        if (ex is ReflectionTypeLoadException reflectedTypeLoad)
        {
            if (reflectedTypeLoad.LoaderExceptions?.Length > 0)
            {
                json.WritePropertyName("loaderExceptions");
                json.WriteStartArray();
                foreach (var loader in reflectedTypeLoad.LoaderExceptions)
                    GetFullDetailsInJson(loader, json);
                json.WriteEndArray();
            }
            if (reflectedTypeLoad.Types?.Length > 0)
            {
                json.WritePropertyName("types");
                json.WriteStartArray();
                foreach (var reflectedTypeLoadType in reflectedTypeLoad.Types)
                    json.WriteValue(reflectedTypeLoadType.FullName);
                json.WriteEndArray();
            }
        }
        else if (ex is AggregateException aggregate && aggregate.InnerExceptions?.Count > 0)
        {
            json.WritePropertyName("innerExceptions");
            json.WriteStartArray();
            foreach (var inner in aggregate.InnerExceptions)
                GetFullDetailsInJson(inner, json);
            json.WriteEndArray();
        }
        else if (ex.InnerException is Exception inner)
        {
            json.WritePropertyName("innerException");
            GetFullDetailsInJson(inner, json);
        }
        json.WriteEndObject();
    }

    static string GetFullDetailsInPlainText(Exception ex, int indent)
    {
        var exceptionMessages = new List<string>();
        var top = true;
        while (ex != default)
        {
            var indentation = new string(' ', indent * 3);
            if (string.IsNullOrWhiteSpace(ex.StackTrace))
                exceptionMessages.Add($"{indentation}{(top ? "-- " : "   ")}{ex.GetType().Name}: {ex.Message}".Replace($"{Environment.NewLine}", $"{Environment.NewLine}{indentation}", StringComparison.OrdinalIgnoreCase));
            else
                exceptionMessages.Add($"{indentation}{(top ? "-- " : "   ")}{ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}".Replace($"{Environment.NewLine}", $"{Environment.NewLine}{indentation}", StringComparison.OrdinalIgnoreCase));
            if (ex is ReflectionTypeLoadException reflectedTypeLoad && reflectedTypeLoad.LoaderExceptions?.Length > 0)
            {
                foreach (var loader in reflectedTypeLoad.LoaderExceptions)
                    exceptionMessages.Add(GetFullDetailsInPlainText(loader, indent + 1));
                break;
            }
            else if (ex is AggregateException aggregate && aggregate.InnerExceptions?.Count > 0)
            {
                foreach (var inner in aggregate.InnerExceptions)
                    exceptionMessages.Add(GetFullDetailsInPlainText(inner, indent + 1));
                break;
            }
            else
                ex = ex.InnerException;
            top = false;
        }
        return string.Join(string.Join("{0}{0}", Environment.NewLine), exceptionMessages);
    }

    static void GetFullDetailsInXml(Exception ex, XmlWriter xml)
    {
        var type = ex.GetType();
        xml.WriteAttributeString("type", type.FullName);
        xml.WriteAttributeString("message", ex.Message);
        if (ex.Data?.Count > 0)
            foreach (var key in ex.Data.Keys)
            {
                xml.WriteStartElement("datum");
                xml.WriteAttributeString("key", key.ToString());
                xml.WriteAttributeString("value", ex.Data[key].ToString());
                xml.WriteEndElement();
            }
        var additionalProperties = GetAdditionalProperties(ex, type);
        if (additionalProperties.Any())
            foreach (var (name, value) in additionalProperties)
            {
                xml.WriteStartElement("property");
                xml.WriteAttributeString("name", name);
                if (value is { })
                    xml.WriteAttributeString("value", value.ToString());
                xml.WriteEndElement();
            }
        var stackTrace = new StackTrace(ex, true);
        xml.WriteStartElement("stackTrace");
        foreach (var frame in stackTrace.GetFrames())
        {
            xml.WriteStartElement("frame");
            var method = frame.GetMethod();
            xml.WriteAttributeString("type", method.DeclaringType.FullName);
            xml.WriteAttributeString("method", GetMethodDescription(method));
            if (frame.GetFileName() is string fileName)
            {
                xml.WriteAttributeString("file", fileName);
                xml.WriteAttributeString("line", frame.GetFileLineNumber().ToString());
                xml.WriteAttributeString("column", frame.GetFileColumnNumber().ToString());
            }
            xml.WriteAttributeString("offset", frame.GetILOffset().ToString());
            xml.WriteEndElement();
        }
        xml.WriteEndElement();
        if (ex is ReflectionTypeLoadException reflectedTypeLoad)
        {
            if (reflectedTypeLoad.LoaderExceptions?.Length > 0)
                foreach (var loader in reflectedTypeLoad.LoaderExceptions)
                {
                    xml.WriteStartElement("loaderException");
                    GetFullDetailsInXml(loader, xml);
                    xml.WriteEndElement();
                }
            if (reflectedTypeLoad.Types?.Length > 0)
                foreach (var reflectedTypeLoadType in reflectedTypeLoad.Types)
                    xml.WriteElementString("type", reflectedTypeLoadType.FullName);
        }
        else if (ex is AggregateException aggregate && aggregate.InnerExceptions?.Count > 0)
            foreach (var inner in aggregate.InnerExceptions)
            {
                xml.WriteStartElement("innerException");
                GetFullDetailsInXml(inner, xml);
                xml.WriteEndElement();
            }
        else if (ex.InnerException is Exception inner)
        {
            xml.WriteStartElement("innerException");
            GetFullDetailsInXml(inner, xml);
            xml.WriteEndElement();
        }
    }

    static string GetMethodDescription(MethodBase method) => $"{method.Name}({string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.FullName} {p.Name}"))})";

    /// <summary>
    /// Gets/sets the default <see cref="ExceptionFullDetailsFormat"/> used by <see cref="GetFullDetails"/>
    /// </summary>
    public static ExceptionFullDetailsFormat DefaultExceptionFullDetailsFormat { get; } = ExceptionFullDetailsFormat.PlainText;
}
