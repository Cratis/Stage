// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

internal static class round_trip_meaning_gates
{
    const string FreshnessMessage = "A generated event source already has facts; retry the command with a fresh generated value.";

    static readonly gate[] Gates =
    [
        new("V1 occurrence identity/context", version => version == SemanticVersion.V1, node => node is SemanticEventContract or SemanticCommand),
        Before("V6 execution phases", SemanticVersion.V6, HasLegacyExecution),
        Before("V8 WholeNumber int32/int64", SemanticVersion.V8, node => node is SemanticPrimitiveType.WholeNumber),
        Before("V8 unordered event matching", SemanticVersion.V8, node => node is SemanticSpecification { ThenEventsInAnyOrder: true }),
        Before("V8 occurrence route admission", SemanticVersion.V8, node => node is SemanticEventContract),
        Before("V11 numeric literal lowering", SemanticVersion.V11, node => node is SemanticNumberValue),
        Before("V15 generated-value checks", SemanticVersion.V15, HasLegacyGeneration),
        new("V32/V33 capture identity", version => version.Major is 32 or 33, node => node is SemanticCapture { EventsSource: not null })
    ];

    public static void EnsurePreserved(ExecutableSemanticModel model)
    {
        if (TryFind(model, out var change))
        {
            throw new InvalidSemanticContract($"Cannot promote ({model.LanguageVersion}, {model.SemanticVersion}) to {round_trip_model_profile.Common}: {change.Name} changes meaning at {change.Path}; no meaning-preserving promotion is proven.");
        }
    }

    public static bool TryFind(ExecutableSemanticModel model, out meaning_change change)
    {
        foreach (var gate in Gates.Where(gate => gate.Changes(model.SemanticVersion)))
        {
            foreach (var node in round_trip_semantic_nodes.Walk(model.Application))
            {
                if (gate.Affected(node.Value))
                {
                    change = new(gate.Name, node.Path);
                    return true;
                }
            }
        }
        change = null!;
        return false;
    }

    static gate Before(string name, SemanticVersion introduction, Func<object, bool> affected) => new(name, version => !version.IsAtLeast(introduction), affected);

    static bool HasLegacyExecution(object node) => node is SemanticReducer or SemanticEventContextExpression or
        SemanticProjection { Scope: not null } or SemanticSpecification { When: not null } or SemanticSpecification { WhenAppended: not null };

    // Conservative boundaries: do not guess numeric lowering or silently strengthen legacy generated-value assertions.
    static bool HasLegacyGeneration(object node)
    {
        if (node is SemanticProperty { IsGenerated: true } or SemanticConcept { Generation: not null }) return true;
        return node is SemanticSpecificationError error && string.Equals(error.Message, FreshnessMessage, StringComparison.Ordinal);
    }

    public sealed record meaning_change(string Name, string Path)
    {
        public string DifferencePath => $"/version-meaning-unproven[{Name}]{Path}";
    }

    sealed record gate(string Name, Func<SemanticVersion, bool> Changes, Func<object, bool> Affected);
}
#endif
