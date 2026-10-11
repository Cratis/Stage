// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

/// <summary>
/// Renders the portable Screenplay stream-id codec used by generated handlers.
/// </summary>
internal static class StreamIdsRendering
{
    internal static RenderedFile Render(string rootNamespace)
    {
        var builder = new CSharpCodeBuilder().Namespace($"{rootNamespace}.GeneratedEventSources")
            .Summary("Formats portable stream identities without rewriting text.")
            .OpenBlock("internal static class StreamIds")
            .Summary("Accepts nonempty, well-formed Unicode NFC text unchanged.")
            .Line("/// <param name=\"value\">The text identity.</param>")
            .Line("/// <param name=\"formatted\">The unchanged valid identity.</param>")
            .Line("/// <returns>Whether the identity is valid.</returns>")
            .OpenBlock("internal static bool TryText(string value, out string formatted)")
            .Line("formatted = string.Empty;")
            .Line("if (value.Length == 0) return false;")
            .OpenBlock("for (var index = 0; index < value.Length; index++)")
            .Line("if (!char.IsSurrogate(value[index])) continue;")
            .OpenBlock("if (char.IsHighSurrogate(value[index]) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))")
            .Line("index++;")
            .EndBlock()
            .OpenBlock("else")
            .Line("return false;")
            .EndBlock()
            .EndBlock()
            .Line("if (!value.IsNormalized(global::System.Text.NormalizationForm.FormC)) return false;")
            .Line("formatted = value;")
            .Line("return true;")
            .EndBlock().BlankLine()
            .Summary("Formats a UUID in lowercase D form.")
            .Line("/// <param name=\"value\">The UUID.</param>")
            .Line("/// <returns>The canonical identity.</returns>")
            .ExpressionMember("internal static string Uuid(global::System.Guid value)", "value.ToString(\"D\", global::System.Globalization.CultureInfo.InvariantCulture)").BlankLine()
            .Summary("Formats a whole number in invariant decimal.")
            .Line("/// <param name=\"value\">The whole number.</param>")
            .Line("/// <returns>The canonical identity.</returns>")
            .ExpressionMember("internal static string Integer(long value)", "value.ToString(global::System.Globalization.CultureInfo.InvariantCulture)").BlankLine()
            .Summary("Escapes canonical parts in declaration order.")
            .Line("/// <param name=\"parts\">The canonical scalar identities.</param>")
            .Line("/// <returns>The composite identity.</returns>")
            .ExpressionMember("internal static string Composite(params string[] parts)", "string.Join('|', global::System.Linq.Enumerable.Select(parts, part => part.Replace(\"%\", \"%25\", global::System.StringComparison.Ordinal).Replace(\"|\", \"%7C\", global::System.StringComparison.Ordinal)))")
            .EndBlock();
        return new(Path.Combine("GeneratedEventSources", "StreamIds.cs"), builder.ToString());
    }
}
