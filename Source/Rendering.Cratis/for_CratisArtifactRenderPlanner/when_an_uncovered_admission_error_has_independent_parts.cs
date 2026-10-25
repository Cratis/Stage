// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_an_uncovered_admission_error_has_independent_parts : given.a_register_project_render_request
{
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        var application = _model.Application with
        {
            Modules = [_module with
            {
                Features = [_feature with
                {
                    Slices = [.. _feature.Slices.Select(slice => slice.Id == _projectLookup.Id ? slice with { Kind = (SemanticSliceKind)999 } : slice)]
                }]
            }]
        };
        var field = typeof(ExecutableSemanticModel).GetField("<Application>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new InvalidOperationException("The semantic application backing field is missing.");
        field.SetValue(_model, application);
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_preserve_the_existing_owner_diagnostic() => _plan.Diagnostics.Any(diagnostic => string.Equals(diagnostic.Code, "STAGE-ESM-001", StringComparison.Ordinal) && diagnostic.Artifact == _projectLookup.Id && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning && string.Equals(diagnostic.Message, "The slice kind is not supported by the Cratis ESM planner.", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_record_a_rendering_gap() => _plan.ImplementationGaps.Any(gap => gap.Kind == ArtifactImplementationGapKind.Rendering && string.Equals(gap.Owner, _projectLookup.Id.ToString(), StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_the_independent_command() => _plan.Artifacts.Any(artifact => artifact.Sources.Contains(_registerProject.Commands.Single().Id)).ShouldBeTrue();
    [Fact] void should_keep_the_independent_specification() => _plan.Artifacts.Any(artifact => artifact.Sources.Contains(_registerProject.Specifications.Single(specification => string.Equals(specification.Name, "RejectingAnEmptyProjectName", StringComparison.Ordinal)).Id)).ShouldBeTrue();
    [Fact] void should_omit_the_unsupported_slice() => _plan.Artifacts.Any(artifact => artifact.Sources.Contains(_projectLookup.ReadModels.Single().Id)).ShouldBeFalse();
    [Fact] void should_be_partial() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
#if DEBUG
    [Fact] void should_compile_the_remaining_output() => given.opaque_policy_model.Compile(_plan).GetTypes().Any(type => string.Equals(type.Name, "RegisterProject", StringComparison.Ordinal)).ShouldBeTrue();
#endif
}
