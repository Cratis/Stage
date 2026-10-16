// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_a_generation_rule_command : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(GenerationFixturesCorpus.V21.EsmBytes.AsSpan())).Plan!;
    void Because() => _admission = new(_plan);

    [Fact] void should_refuse_only_the_command_that_generates_a_rule_value() => _admission.Blocking.Single().Artifact.ShouldEqual(_plan.Commands.Values.Single(command => command.Name == "TenantValue").Id.ToString());
    [Fact] void should_name_the_interpreter_boundary_without_inventing_a_rule_name() => _admission.Blocking.Single().Details.ShouldEqual("STAGE-ESM-028: Command 'TenantValue' generates 'value' with 'generate rule' on concept 'TenantOrderId'; Stage's interpreter never runs implementation bodies — render the application.");
    [Fact] void should_identify_the_command_capability() => _admission.Blocking.Single().Kind.ShouldEqual("command");
}
