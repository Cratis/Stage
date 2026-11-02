// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Resources;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Realization;

/// <summary>
/// Reads the source of the files an application rendered with authentication or onboarding gets.
/// </summary>
/// <remarks>
/// The templates are ordinary C# that Stage's own specifications compile together with the files rendered from them,
/// so a template that stops compiling fails here and not in someone's application.
/// </remarks>
internal static class RealizationTemplates
{
    const string Prefix = "Cratis.Stage.Rendering.Cratis.Realization.";

    internal static string Read(string name, string rootNamespace, params (string Token, string Value)[] tokens)
    {
        using var stream = typeof(RealizationTemplates).Assembly.GetManifestResourceStream($"{Prefix}{name}.cs.txt") ??
            throw new MissingManifestResourceException($"The realization template '{name}' is not embedded.");
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
        text = text.Replace("__Root__", rootNamespace, StringComparison.Ordinal);
        foreach (var (token, value) in tokens)
        {
            text = text.Replace(token, value, StringComparison.Ordinal);
        }

        return text;
    }

    internal static string Literal(string value) => CSharpCodeBuilder.StringLiteral(value);

    internal static string List(IEnumerable<string> values) => $"[{string.Join(", ", values.Select(Literal))}]";
}
