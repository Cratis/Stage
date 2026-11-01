// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_degrading_an_owner;

public class without_unsupported_member_error_provenance : given.an_unsupported_event_request
{
    ArtifactRenderPlan _refused = null!;
    ITuple _result = null!;

    void Because()
    {
        var context = new SemanticApplicationContext(_request, _options);
        var warning = new ArtifactRenderDiagnostic("STAGE-ESM-012", ArtifactRenderDiagnosticSeverity.Warning, "Unsupported member warning.", _unsupportedEvent);
        context.OwnerScopedRefusals.Add(warning);
        List<ArtifactRenderDiagnostic> diagnostics = [warning, new("STAGE-ESM-004", ArtifactRenderDiagnosticSeverity.Error, "Unrelated whole refusal.", _unsupportedEvent)];
        _refused = ArtifactRenderPlan.Create(_request, [], [.. diagnostics]);
        var planner = typeof(CratisArtifactRenderPlanner);
        var failure = Activator.CreateInstance(planner.GetNestedType("OwnerOmissionFailure", BindingFlags.NonPublic)!, context, diagnostics, ImmutableArray<ArtifactRemoval>.Empty);

        // Any dependency-index construction would dereference this poisoned view and fail the spec.
        typeof(SemanticApplicationContext).GetField("_slices", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(context, null);
        _result = (ITuple)planner.GetMethod("RetryOmission", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [_request, _options, failure, _refused])!;
    }

    [Fact] void should_return_the_original_refusal_without_building_an_index() => Assert.Same(_refused, _result[0]);
    [Fact] void should_not_attempt_a_second_pass_for_warning_only_provenance() => _result[1].ShouldBeNull();
}
