// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_refusing_event_reactions : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;

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
        _plan = SemanticExecutionPlan.Compile(model).Plan!;
    }

    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_plan, new([.. _plan.Specifications.Keys]), new());

    [Fact] void should_run_no_reaction_scenarios() => _report.Results.All(result => result.Trace is null).ShouldBeTrue();
    [Fact] void should_refuse_every_specification() => _report.Results.Select(result => result.Outcome).Distinct().ShouldContainOnly([SemanticSpecificationOutcome.Unsupported]);
    [Fact] void should_name_reaction_admission_before_the_automation_slice() => _report.Results.Select(result => result.Unsupported!.Details).Distinct().ShouldContainOnly(["STAGE-ESM-024: Reaction 'ReminderScheduler' is an ESM v6 automation construct; the Stage Host and specification executor do not run reactions yet."]);
    [Fact] void should_report_the_reaction_capability() => _report.Results.Select(result => result.Unsupported!.Capability).Distinct().ShouldContainOnly([StageExecutionCapability.Reaction]);
}
