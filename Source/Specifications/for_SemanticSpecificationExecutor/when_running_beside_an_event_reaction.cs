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

    [Fact] void should_run_the_validation_specification() => _report.Results.Single(result => result.Name == "RejectingAnEmptyReason").Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_keep_the_clock_specification_refused() => _report.Results.Single(result => result.Name == "SendingAnInvoice").Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_name_the_clock_refusal() => Assert.StartsWith("STAGE-ESM-024:", _report.Results.Single(result => result.Name == "SendingAnInvoice").Unsupported!.Details, StringComparison.Ordinal);
}
