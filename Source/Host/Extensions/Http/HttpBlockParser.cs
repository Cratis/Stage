// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// Reads the <c lang="text">http</c> block of an ability: structured lines, never JSON.
/// </summary>
public static partial class HttpBlockParser
{
    /// <summary>
    /// The names of the call properties a value can read.
    /// </summary>
    public static readonly IReadOnlyList<string> CallProperties = ["idempotencyKey", "attempt", "tenant", "system", "ability"];

    /// <summary>
    /// The headers a block can never set, because credentials come from the settings and secrets of the system and are never in the model.
    /// </summary>
    public static readonly IReadOnlyList<string> ReservedHeaders = ["Authorization", "Proxy-Authorization", "Cookie", "X-API-Key", "Host"];

    static readonly string[] _bodiless = ["get", "delete"];

    [GeneratedRegex("^(?<method>get|post|put|patch|delete)\\s+\"(?<path>[^\"]*)\"$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex MethodLine { get; }

    [GeneratedRegex("^response\\s+(?<status>\\d{3})(?:\\s*->\\s*(?<outcome>[A-Za-z_][A-Za-z0-9_]*))?$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex ResponseLine { get; }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*(?:\\.[A-Za-z_][A-Za-z0-9_]*)*$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex PathValue { get; }

    [GeneratedRegex("^(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*(?<value>.+)$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Assignment { get; }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Name { get; }

    [GeneratedRegex("^header\\s+\"(?<name>[^\"]+)\"\\s*=\\s*(?<value>.+)$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex HeaderLine { get; }

    [GeneratedRegex("^query\\s+\"(?<name>[^\"]+)\"\\s*=\\s*(?<value>.+)$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex QueryLine { get; }

    /// <summary>
    /// Reads a block.
    /// </summary>
    /// <param name="system">The name of the system the ability belongs to.</param>
    /// <param name="ability">The name of the ability.</param>
    /// <param name="block">The text of the block, with or without its leading <c lang="text">http</c> line.</param>
    /// <returns>The <see cref="HttpBlockParse"/>.</returns>
    public static HttpBlockParse Parse(string system, string ability, string block)
    {
        var errors = new List<string>();
        var lines = Lines(block);
        if (lines.Count > 0 && string.Equals(lines[0].Text, "http", StringComparison.Ordinal)) lines.RemoveAt(0);
        if (lines.Count == 0) return new(null, ["The http block is empty: it needs a method and a path."]);

        string? method = null;
        string? path = null;
        var headers = new List<HttpNamedValue>();
        var query = new List<HttpNamedValue>();
        var body = new List<HttpBodyField>();
        var responses = new List<HttpResponseRule>();
        var baseIndent = lines[0].Indent;

        var index = 0;
        while (index < lines.Count)
        {
            var line = lines[index];
            var children = Children(lines, index);
            index += 1 + children.Count;
            if (line.Indent != baseIndent)
            {
                errors.Add($"Line {line.Number}: unexpected indentation.");
                continue;
            }

            var text = line.Text;
            if (MethodLine.Match(text) is { Success: true } methodMatch)
            {
                if (method is not null) errors.Add($"Line {line.Number}: the block already says {method.ToUpperInvariant()} {path}.");
                method = methodMatch.Groups["method"].Value;
                path = methodMatch.Groups["path"].Value;
                Refuse(children, line, "a method line takes no nested lines", errors);
                ValidatePath(path, line, errors);
            }
            else if (HeaderLine.Match(text) is { Success: true } headerMatch)
            {
                ReadNamed(headerMatch, line, headers, errors, false);
                if (ReservedHeaders.Contains(headerMatch.Groups["name"].Value, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"Line {line.Number}: the header {headerMatch.Groups["name"].Value} is set from the settings and secrets of the system, never from the block.");
                }
            }
            else if (QueryLine.Match(text) is { Success: true } queryMatch)
            {
                ReadNamed(queryMatch, line, query, errors, false);
            }
            else if (string.Equals(text, "body", StringComparison.Ordinal))
            {
                body.AddRange(Fields(children, errors));
            }
            else if (ResponseLine.Match(text) is { Success: true } responseMatch)
            {
                responses.Add(ReadResponse(responseMatch, children, errors, responses));
            }
            else
            {
                errors.Add($"Line {line.Number}: '{text}' is not a method, header, query, body or response line.");
            }
        }

        if (method is null || path is null) errors.Add("The http block needs a method and a path, for example: post \"/pickups\".");
        if (body.Count > 0 && _bodiless.Contains(method, StringComparer.Ordinal)) errors.Add($"A {method?.ToUpperInvariant()} request cannot have a body.");
        if (responses.Count == 0) errors.Add("The http block needs at least one response line.");
        if (errors.Count > 0) return new(null, errors);

        return new(new HttpAbilityDefinition(system, ability, method!.ToUpperInvariant(), path!, headers, query, body, responses), []);
    }

    /// <summary>
    /// Reads a value written in a block.
    /// </summary>
    /// <param name="text">The text of the value.</param>
    /// <param name="inResponse">Whether the value is read from a response rather than from the call.</param>
    /// <returns>The value, or null when the text is not a value.</returns>
    public static HttpValue? ParseValue(string text, bool inResponse)
    {
        text = text.Trim();
        if (text.Length >= 2 && text[0] == '"' && text[^1] == '"' && !text[1..^1].Contains('"', StringComparison.Ordinal))
        {
            return new(HttpValueKind.Literal, text[1..^1], JsonValue.Create(text[1..^1]));
        }

        if (string.Equals(text, "true", StringComparison.Ordinal) || string.Equals(text, "false", StringComparison.Ordinal)) return new(HttpValueKind.Literal, text, JsonValue.Create(string.Equals(text, "true", StringComparison.Ordinal)));
        if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var whole)) return new(HttpValueKind.Literal, text, JsonValue.Create(whole));
        if (decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number)) return new(HttpValueKind.Literal, text, JsonValue.Create(number));

        if (text.StartsWith("$call.", StringComparison.Ordinal))
        {
            var property = text["$call.".Length..];
            return CallProperties.Contains(property) ? new(HttpValueKind.Call, property) : null;
        }

        if (!PathValue.IsMatch(text)) return null;
        if (!inResponse) return new(HttpValueKind.Input, text);
        var isResponsePath = string.Equals(text, "status", StringComparison.Ordinal) || string.Equals(text, "body", StringComparison.Ordinal) || text.StartsWith("body.", StringComparison.Ordinal);
        return isResponsePath ? new(HttpValueKind.Response, text) : null;
    }

    static List<Line> Lines(string block)
    {
        var lines = new List<Line>();
        var number = 0;
        foreach (var raw in block.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            number++;
            var text = raw.Trim();
            if (text.Length == 0) continue;
            lines.Add(new(number, raw.Length - raw.TrimStart().Length, text));
        }

        return lines;
    }

    static List<Line> Children(List<Line> lines, int index)
    {
        var children = new List<Line>();
        for (var next = index + 1; next < lines.Count && lines[next].Indent > lines[index].Indent; next++) children.Add(lines[next]);
        return children;
    }

    static void Refuse(List<Line> children, Line line, string reason, List<string> errors)
    {
        if (children.Count > 0) errors.Add($"Line {line.Number}: {reason}.");
    }

    static void ValidatePath(string path, Line line, List<string> errors)
    {
        if (!path.StartsWith('/') || path.StartsWith("//", StringComparison.Ordinal) || path.Contains("://", StringComparison.Ordinal) || path.Contains("..", StringComparison.Ordinal) || path.Contains('?', StringComparison.Ordinal))
        {
            errors.Add($"Line {line.Number}: the path must start with a single / and stay under the base address of the system; put parameters in query lines.");
        }
    }

    static void ReadNamed(Match match, Line line, List<HttpNamedValue> into, List<string> errors, bool inResponse)
    {
        var value = ParseValue(match.Groups["value"].Value, inResponse);
        if (value is null)
        {
            errors.Add($"Line {line.Number}: '{match.Groups["value"].Value}' is not a value.");
            return;
        }

        into.Add(new(match.Groups["name"].Value, value));
    }

    static List<HttpBodyField> Fields(List<Line> lines, List<string> errors)
    {
        var fields = new List<HttpBodyField>();
        if (lines.Count == 0) return fields;
        var indent = lines[0].Indent;
        var index = 0;
        while (index < lines.Count)
        {
            var line = lines[index];
            var children = Children(lines, index);
            index += 1 + children.Count;
            if (line.Indent != indent)
            {
                errors.Add($"Line {line.Number}: unexpected indentation.");
                continue;
            }

            if (Assignment.Match(line.Text) is { Success: true } assignment)
            {
                Refuse(children, line, "a field with a value takes no nested fields", errors);
                var value = ParseValue(assignment.Groups["value"].Value, false);
                if (value is null) errors.Add($"Line {line.Number}: '{assignment.Groups["value"].Value}' is not a value.");
                else fields.Add(new(assignment.Groups["name"].Value, value, []));
            }
            else if (Name.IsMatch(line.Text) && children.Count > 0)
            {
                fields.Add(new(line.Text, null, Fields(children, errors)));
            }
            else
            {
                errors.Add($"Line {line.Number}: '{line.Text}' is not a body field: write name = value, or a name with nested fields.");
            }
        }

        return fields;
    }

    static HttpResponseRule ReadResponse(Match match, List<Line> children, List<string> errors, List<HttpResponseRule> existing)
    {
        var status = int.Parse(match.Groups["status"].Value, CultureInfo.InvariantCulture);
        if (status is < 100 or > 599 || existing.Exists(rule => rule.Status == status)) errors.Add($"The response status {status} is not valid or is written twice.");
        var outcome = match.Groups["outcome"].Success ? match.Groups["outcome"].Value : null;
        var output = new List<HttpNamedValue>();
        foreach (var child in children)
        {
            if (Assignment.Match(child.Text) is { Success: true } assignment) ReadNamed(assignment, child, output, errors, true);
            else errors.Add($"Line {child.Number}: '{child.Text}' is not a returned value: write name = body.path.");
        }

        return new(status, outcome, output);
    }

    sealed record Line(int Number, int Indent, string Text);
}
