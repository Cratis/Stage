// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatureAdmission;

public class when_rendering_event_reactions : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        var model = SemanticModelSerializer.Deserialize(ReactionsCorpus.V6.EsmBytes.AsSpan());
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var reminders = feature.Slices.Single(slice => slice.Kind == SemanticSliceKind.Automation);
        reminders = reminders with { Reactions = [reminders.Reactions.Single(reaction => reaction.Name == "ReminderScheduler")], Specifications = [] };
        model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Triggers = [],
            Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Where(slice => slice.Kind == SemanticSliceKind.StateChange).Select(slice => slice with { Specifications = [] }), reminders] }] }]
        });
        _model = model;
    }

    void Because() => _plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model).Plan, new(ArtifactRenderScopeKind.Application, _model.Application.Id), new("Reactions", "Reactions"));

    [Fact] void should_admit_event_reactions() => _plan.Success.ShouldBeTrue();
    [Fact] void should_emit_the_reactor() => _plan.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("/ReminderScheduler.cs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_emit_no_refusals() => _plan.Diagnostics.Where(diagnostic => diagnostic.Severity != ArtifactRenderDiagnosticSeverity.Information).ShouldBeEmpty();
}
