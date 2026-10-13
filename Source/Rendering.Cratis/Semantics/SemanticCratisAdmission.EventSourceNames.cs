// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    static readonly string[] _eventSourceMembers = ["Equals", "GetHashCode", "GetType", "ToString", "ReferenceEquals", "MemberwiseClone", "Finalize"];

    [GeneratedRegex(@"^[A-Z]\w*$", RegexOptions.None, 1000)]
    private static partial Regex StreamIdentifier { get; }

    static void ValidateEventSourceNames(SemanticEventSource source, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var reserved = _eventSourceMembers.Append(SemanticEventSourceArtifactRenderer.ClassName(source)).ToHashSet(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var stream in source.Streams)
        {
            if (!IsStreamIdentifier(stream.Name) || reserved.Contains(stream.Name) || !seen.Add(stream.Name))
            {
                diagnostics.Add(Error("STAGE-ESM-012", $"Stream '{stream.Name}' cannot be preserved as a unique Screenplay identifier on its generated event-source class.", stream.Id));
            }
        }
    }

    static bool IsStreamIdentifier(string name) =>
        StreamIdentifier.IsMatch(name) && SyntaxFacts.IsValidIdentifier(name) &&
        SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None && SyntaxFacts.GetContextualKeywordKind(name) == SyntaxKind.None;
}
