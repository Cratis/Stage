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

public class when_refusing_reactions : Specification
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
            Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Where(slice => slice.Kind == SemanticSliceKind.StateChange), reminders] }] }]
        });
        _model = model;
    }

    void Because() => _plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model).Plan!, new(ArtifactRenderScopeKind.Application, _model.Application.Id), new("Reactions", "Reactions"));

    [Fact] void should_keep_event_reactions_refused() => _plan.Success.ShouldBeFalse();
    [Fact] void should_emit_nothing() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_name_the_reaction_refusal() => _plan.Diagnostics.Single(diagnostic => diagnostic.Artifact == _model.Application.Modules[0].Features[0].Slices[^1].Reactions.Single().Id).Message.ShouldEqual("Reaction 'ReminderScheduler' is an ESM v6 automation construct; reaction rendering is not yet admitted.");
    [Fact] void should_report_the_automation_code() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly(["STAGE-ESM-024"]);
}
