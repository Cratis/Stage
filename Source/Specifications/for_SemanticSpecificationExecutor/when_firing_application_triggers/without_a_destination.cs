// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_firing_application_triggers;

public class without_a_destination : when_settling_reactions.given.a_reaction_plan
{
    Exception _failure;

    void Establish() => Compile("reaction Worker\n  when Startup\n    produces Completed\n      for \"one\"", "then Completed", new(Action: "when trigger Startup"));

    void Because()
    {
        var application = _plan.Model.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var reaction = slice.Reactions.Single();
        var trigger = reaction.Triggers.Single();
        var changed = reaction with { Triggers = [trigger with { Produces = [trigger.Produces.Single() with { Destination = null, DestinationType = null }] }] };
        application = application with { Modules = [module with { Features = [feature with { Slices = [slice with { Reactions = [changed] }] }] }] };
        _failure = Catch.Exception(() => ExecutableSemanticModel.Create(_plan.Model.LanguageVersion, _plan.Model.SemanticVersion, application));
    }

    [Fact] void should_reject_the_invalid_contract_before_it_can_be_executed() => _failure.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_explain_the_required_destination() => _failure.Message.ShouldEqual("Reaction 'Worker' must say with 'for' which event source it appends to, unless an event sets it off.");
}
