namespace UnboundOS.Infrastructure.Mods;

/// <summary>
/// Small, read-only parser for Valve's text KeyValues format used by Steam manifests.
/// It intentionally supports only quoted/bare tokens and nested objects; binary VDF is ignored.
/// </summary>
internal static class ValveKeyValuesParser
{
    public static ValveNode Parse(string text)
    {
        var tokens = Tokenize(text).ToArray();
        var index = 0;
        var root = new ValveNode("root");

        while (index < tokens.Length)
        {
            ParsePair(root, tokens, ref index);
        }

        return root;
    }

    private static void ParsePair(ValveNode parent, IReadOnlyList<string> tokens, ref int index)
    {
        if (index >= tokens.Count || tokens[index] == "}")
        {
            index++;
            return;
        }

        var key = tokens[index++];
        if (index >= tokens.Count)
        {
            return;
        }

        if (tokens[index] == "{")
        {
            index++;
            var child = new ValveNode(key);
            while (index < tokens.Count && tokens[index] != "}")
            {
                ParsePair(child, tokens, ref index);
            }

            if (index < tokens.Count && tokens[index] == "}")
            {
                index++;
            }

            parent.Children.Add(child);
            return;
        }

        parent.Values[key] = tokens[index++];
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        var index = 0;
        while (index < text.Length)
        {
            if (char.IsWhiteSpace(text[index]))
            {
                index++;
                continue;
            }

            if (text[index] == '/' && index + 1 < text.Length && text[index + 1] == '/')
            {
                index += 2;
                while (index < text.Length && text[index] is not '\r' and not '\n')
                {
                    index++;
                }

                continue;
            }

            if (text[index] is '{' or '}')
            {
                yield return text[index++].ToString();
                continue;
            }

            if (text[index] == '"')
            {
                index++;
                var value = new System.Text.StringBuilder();
                while (index < text.Length && text[index] != '"')
                {
                    if (text[index] == '\\' && index + 1 < text.Length)
                    {
                        var escaped = text[index + 1];
                        if (escaped is '"' or '\\')
                        {
                            value.Append(escaped);
                            index += 2;
                            continue;
                        }
                    }

                    value.Append(text[index++]);
                }

                if (index < text.Length)
                {
                    index++;
                }

                yield return value.ToString();
                continue;
            }

            var start = index;
            while (index < text.Length && !char.IsWhiteSpace(text[index]) && text[index] is not '{' and not '}')
            {
                index++;
            }

            yield return text[start..index];
        }
    }
}

internal sealed class ValveNode(string name)
{
    public string Name { get; } = name;
    public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<ValveNode> Children { get; } = [];

    public ValveNode? Child(string name) =>
        Children.FirstOrDefault(child => child.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public string? Value(string key) =>
        Values.TryGetValue(key, out var value) ? value : null;
}
