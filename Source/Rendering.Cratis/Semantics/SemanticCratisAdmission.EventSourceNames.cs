// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    static readonly string[] _eventSourceMembers = ["Equals", "GetHashCode", "GetType", "ToString", "ReferenceEquals", "MemberwiseClone", "Finalize"];

    static void ValidateEventSourceNames(SemanticEventSource source, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var reserved = _eventSourceMembers.Append(SemanticEventSourceArtifactRenderer.ClassName(source)).ToHashSet(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var stream in source.Streams.Where(stream => SemanticEventSourceArtifactRenderer.IsIdentityName(stream.Name)))
        {
            if (reserved.Contains(stream.Name) || !seen.Add(stream.Name))
            {
                diagnostics.Add(Error("STAGE-ESM-012", $"Stream '{stream.Name}' cannot be preserved as a unique Screenplay identifier on its generated event-source class.", stream.Id));
            }
        }
    }
}
