// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Validation;

internal static class PortableMatchRendering
{
    internal static string Predicate(string pattern, string value = "value") => $"_matchPatterns[{CSharpCodeBuilder.StringLiteral(pattern)}].IsMatch({value})";

    internal static void RenderFields(CSharpCodeBuilder builder, IEnumerable<string> patterns)
    {
        var distinct = patterns.Distinct(StringComparer.Ordinal).ToArray();
        if (distinct.Length == 0) return;
        const string matcher = "global::Cratis.Screenplay.Matching.PortableMatcher";
        builder.Line($"static readonly global::System.Collections.Generic.Dictionary<string, {matcher}> _matchPatterns = new(global::System.StringComparer.Ordinal)")
            .Line("{");
        foreach (var pattern in distinct)
        {
            var literal = CSharpCodeBuilder.StringLiteral(pattern);
            builder.Line($"    [{literal}] = {matcher}.Compile({literal}),");
        }

        builder.Line("};").BlankLine();
    }
}
