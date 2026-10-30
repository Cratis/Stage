// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_an_event_reaction_body : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish() => _plan = compiled_plan.From("""
        module Work
          feature Completion
            slice StateChange StartWork
              command StartWork
                id String identifier
                produces Started
                  for id
                  id = id
              event Started
                id String
            slice Automation Notifications
              reaction Notifier
                when Started
                  ```csharp
                  return null;
                  ```
        """);

    void Because() => _admission = new(_plan);

    [Fact] void should_block_exactly_one_reaction() => _admission.Blocking.Where(entry => entry.Kind == "reaction").Select(entry => entry.Artifact).Distinct().Count().ShouldEqual(1);
    [Fact] void should_block_the_authored_reaction() => _admission.Blocking.Single(entry => entry.Kind == "reaction").Artifact.ShouldEqual(_plan.Reactions.Single(reaction => reaction.Name == "Notifier").Id.ToString());
    [Fact] void should_name_the_opaque_body_for_the_event_trigger() => _admission.Blocking.Single(entry => entry.Kind == "reaction").Details!.StartsWith("STAGE-ESM-024: Reaction 'Notifier' has an opaque body for trigger kind '", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_block_the_command() => _admission.Blocking.Any(entry => entry.Kind == "command").ShouldBeFalse();
    [Fact] void should_not_block_anything_else() => _admission.Blocking.Count.ShouldEqual(1);
}
