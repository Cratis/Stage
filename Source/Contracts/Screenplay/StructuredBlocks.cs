// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Writes JSON values as the indented blocks Screenplay uses for objects and lists.
/// </summary>
/// <remarks>
/// Screenplay 5 has no JSON values: an object or a list is an indented block under the line that names it, one
/// <c>member = value</c> line per member and one <c>item</c> line per element, with <c>none</c> for an empty list and <c>empty</c>
/// for an object without members. Stage lowers JSON it receives from callers and specifications to that form with this type.
/// </remarks>
public static class StructuredBlocks
{
    /// <summary>
    /// Writes <c>name = value</c>, or the block form when the value is an object or a list.
    /// </summary>
    /// <param name="name">The member or property name.</param>
    /// <param name="json">The value as JSON text (or a value whose text is JSON); text that is not JSON is written unchanged after the equals sign.</param>
    /// <param name="indent">The number of spaces the first line is indented by.</param>
    /// <returns>One or more lines without a trailing newline; every line after the first is indented relative to <paramref name="indent"/>.</returns>
    public static string Assign(string name, object? json, int indent)
    {
        var text = System.Convert.ToString(json, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        JsonElement value;
        try
        {
            using var document = JsonDocument.Parse(text);
            value = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return $"{name} = {text}";
        }

        var lines = new List<string>();
        Write(lines, name, value, 0);
        return string.Join('\n' + new string(' ', indent), lines);
    }

    static void Write(List<string> lines, string header, JsonElement value, int depth)
    {
        var pad = new string(' ', depth * 2);
        switch (value.ValueKind)
        {
            case JsonValueKind.Object when !value.EnumerateObject().Any():
                lines.Add($"{pad}{header} empty");
                break;
            case JsonValueKind.Object:
                lines.Add($"{pad}{header}");
                foreach (var member in value.EnumerateObject())
                {
                    Write(lines, MemberName(member.Name), member.Value, depth + 1);
                }

                break;
            case JsonValueKind.Array when value.GetArrayLength() == 0:
                lines.Add($"{pad}{header} none");
                break;
            case JsonValueKind.Array:
                lines.Add($"{pad}{header}");
                foreach (var element in value.EnumerateArray())
                {
                    WriteItem(lines, element, depth + 1);
                }

                break;
            default:
                lines.Add($"{pad}{header} = {Scalar(value)}");
                break;
        }
    }

    static void WriteItem(List<string> lines, JsonElement value, int depth)
    {
        var pad = new string(' ', depth * 2);
        switch (value.ValueKind)
        {
            case JsonValueKind.Object or JsonValueKind.Array:
                Write(lines, "item", value, depth);
                break;
            default:
                lines.Add($"{pad}item {Scalar(value)}");
                break;
        }
    }

    static string MemberName(string name) => name switch
    {
        "item" => "@item",
        _ when name.Length > 0 && (char.IsLetter(name[0]) || name[0] == '_') && name.All(character => char.IsLetterOrDigit(character) || character == '_') => name,
        _ => JsonSerializer.Serialize(name)
    };

    static string Scalar(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => JsonSerializer.Serialize(value.GetString()),
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => "null"
    };
}
