// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_beside_an_event_reaction : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;
    SemanticSpecificationRun _reference = null!;

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

    async Task Because()
    {
        _reference = new SemanticSpecificationRunner().Run(_plan, _plan.Specifications.Values.Single(specification => specification.Name == "SendingAnInvoice").Id);
        _report = await new SemanticSpecificationExecutor().Run(_plan, new([.. _plan.Specifications.Keys]), new());
    }

    [Fact] void should_run_the_validation_specification() => _report.Results.Single(result => result.Name == "RejectingAnEmptyReason").Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_match_the_clocked_reference_specification() => _reference.Passed.ShouldBeTrue();
    [Fact] void should_run_the_clocked_reaction_specification() => _report.Results.Single(result => result.Name == "SendingAnInvoice").Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_preserve_the_authored_instant_in_the_reminder() => _report.Results.Single(result => result.Name == "SendingAnInvoice").Trace!.Facts[^1].Values.Values.ShouldContain("\"2026-10-02T09:00:00.0000000Z\"");
}
