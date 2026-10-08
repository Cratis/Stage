// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Refuses each ESM v5–v7 construct the Cratis planner does not render yet, before any artifact is planned.
/// </summary>
/// <remarks>
/// Admitting a version by number never admits its constructs: a selected slice that uses one refuses the
/// whole plan with a diagnostic naming the construct, rather than rendering without it. A model that uses
/// none of them renders exactly as an earlier version would.
/// </remarks>
internal static class SemanticVersionFeatureAdmission
{
    /// <summary>
    /// The diagnostic for ESM v6 triggers, reactions, captures, automation and translate slices and their specification forms.
    /// </summary>
    internal const string Automation = "STAGE-ESM-024";

    /// <summary>
    /// The diagnostic for ESM v5 keyed read-model absence expectations.
    /// </summary>
    internal const string Absence = "STAGE-ESM-027";

    /// <summary>
    /// The diagnostic for ESM v7 generated command values and generation fixtures.
    /// </summary>
    internal const string Generated = "STAGE-ESM-028";

    /// <summary>
    /// The diagnostic for ESM v7 command responses and return expectations.
    /// </summary>
    internal const string Responses = "STAGE-ESM-029";

    /// <summary>
    /// Finds every unrendered v5–v7 construct in the selected scope.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="slices">The selected slices.</param>
    /// <returns>One blocking diagnostic per construct, in model order.</returns>
    public static ImmutableArray<ArtifactRenderDiagnostic> Verify(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices)
    {
        var diagnostics = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();

        // Application triggers belong to no slice; an application render would otherwise drop them.
        if (context.Request.Scope.Kind == ArtifactRenderScopeKind.Application)
        {
            foreach (var trigger in Items(context.Application.Triggers))
            {
                diagnostics.Add(Error(Automation, $"Application trigger '{trigger.Name}' is an ESM v6 automation construct, which Stage does not render yet.", trigger.Id));
            }
        }

        foreach (var slice in slices.Select(located => located.Slice))
        {
            if (slice.Kind is SemanticSliceKind.Automation or SemanticSliceKind.Translate)
            {
                diagnostics.Add(Error(Automation, $"Slice '{slice.Name}' is an ESM v6 {slice.Kind} slice; Stage renders only StateChange and StateView slices.", slice.Id));
            }

            foreach (var reaction in Items(slice.Reactions))
            {
                diagnostics.Add(Error(Automation, $"Reaction '{reaction.Name}' is an ESM v6 automation construct, which Stage does not render yet.", reaction.Id));
            }

            foreach (var capture in Items(slice.Captures))
            {
                diagnostics.Add(Error(Automation, $"Capture '{capture.Name}' is an ESM v6 translation construct, which Stage does not render yet.", capture.Id));
            }

            foreach (var command in slice.Commands)
            {
                foreach (var property in command.Properties.Where(property => property.IsGenerated))
                {
                    diagnostics.Add(Error(Generated, $"Command '{command.Name}' generates '{property.Name}' (ESM v7), which Stage does not render yet.", command.Id));
                }

                if (command.Response is not null)
                {
                    diagnostics.Add(Error(Responses, $"Command '{command.Name}' declares a response (ESM v7), which Stage does not render yet.", command.Id));
                }
            }

            foreach (var specification in slice.Specifications)
            {
                if (specification.GivenClock is not null || specification.WhenClock is not null || specification.WhenTrigger is not null ||
                    specification.WhenCapture is not null || !specification.GivenCaptures.IsDefaultOrEmpty)
                {
                    diagnostics.Add(Error(Automation, $"Specification '{specification.Name}' uses an ESM v6 clock, trigger or capture, which Stage does not render yet.", specification.Id));
                }

                if (!specification.ThenAbsentReadModels.IsDefaultOrEmpty)
                {
                    diagnostics.Add(Error(Absence, $"Specification '{specification.Name}' asserts a read model is absent (ESM v5); the generated ReadModelScenario exposes only a materialized record, so Stage does not render absence yet.", specification.Id));
                }

                if (specification.When is { GeneratedValues.IsDefaultOrEmpty: false })
                {
                    diagnostics.Add(Error(Generated, $"Specification '{specification.Name}' supplies generated values (ESM v7), which Stage does not render yet.", specification.Id));
                }

                if (specification.ThenReturns is not null)
                {
                    diagnostics.Add(Error(Responses, $"Specification '{specification.Name}' asserts a command response (ESM v7), which Stage does not render yet.", specification.Id));
                }
            }
        }

        return diagnostics.ToImmutable();
    }

    static ImmutableArray<T> Items<T>(ImmutableArray<T> items) => items.IsDefault ? [] : items;

    static ArtifactRenderDiagnostic Error(string code, string message, SemanticId artifact) =>
        new(code, ArtifactRenderDiagnosticSeverity.Error, message, artifact);
}
