// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Scene.Model.Forms;

namespace Cratis.Stage.Api;

internal sealed record CommandFormMetadata(
    string Command,
    string Schema,
    IReadOnlyList<CommandFormField> Fields)
{
    internal static CommandFormMetadata FromSchema(string command, string schema) => new(command, schema, CommandFormSchema.FieldsFrom(schema));
}

internal sealed record CommandFormField(
    string Name,
    string Label,
    string? SourceProperty = null,
    string? ComposeUsing = null,
    FormFieldPlacement? Placement = null)
{
    internal static CommandFormField From(FormField field) => new(
        field.Name,
        string.IsNullOrWhiteSpace(field.Label) ? CommandFormSchema.Humanize(field.Name) : field.Label,
        field.SourceProperty,
        field.ComposeUsing,
        field.Placement);
}

internal static class CommandFormSchema
{
    internal static IReadOnlyList<CommandFormField> FieldsFrom(string schema)
    {
        if (string.IsNullOrWhiteSpace(schema))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(schema);
            if (!document.RootElement.TryGetProperty("properties", out var properties) ||
                properties.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            return [.. properties.EnumerateObject().Select(property => new CommandFormField(property.Name, Humanize(property.Name)))];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    internal static string Humanize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var text = new System.Text.StringBuilder(value.Length * 2);
        text.Append(char.ToUpperInvariant(value[0]));
        for (var index = 1; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsUpper(character) && !char.IsUpper(value[index - 1]))
            {
                text.Append(' ');
            }

            text.Append(character);
        }

        return text.ToString();
    }
}
