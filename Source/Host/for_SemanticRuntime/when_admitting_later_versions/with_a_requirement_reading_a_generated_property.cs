// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_a_requirement_reading_a_generated_property : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish()
    {
        var model = compiled_plan.From("""
            concept Id : Uuid
            module M
              feature F
                slice StateChange S
                  command Create
                    id Id generated identifier
                    returns id
            """).Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        var generated = command.Properties.Single();
        var condition = new SemanticComparison(new(generated.Id, null), SemanticComparisonOperator.Equal, new(generated.Id, null));
        var changed = slice with { Commands = [command with { Requirements = [new(condition, null)] }] };
        var application = model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [changed] }] }]
        };

        // Fault injection bypasses the typed-model validator, which rejects generated reads.
        typeof(ExecutableSemanticModel).GetField("<Application>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, application);
        _plan = SemanticExecutionPlan.Compile(model).Plan!;
    }

    void Because() => _admission = new(_plan);

    [Fact] void should_refuse_the_hand_built_command_with_the_shared_diagnostic() => _admission.Blocking.ShouldContain(entry => entry.Artifact == _plan.Commands.Keys.Single().ToString() && entry.Details!.StartsWith("STAGE-ESM-038:", StringComparison.Ordinal));
}
