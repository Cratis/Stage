// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_a_generation_rule_command : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;
    string Requirement => _plan.Model.Application.Concepts.Single(concept => concept.Name == "TenantOrderId").Generation!.RequirementId!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(GenerationFixturesCorpus.V21.EsmBytes.AsSpan())).Plan!;
    void Because() => _admission = new(_plan);

    [Fact] void should_refuse_only_the_command_that_generates_a_rule_value() => _admission.Blocking.Single().Artifact.ShouldEqual(_plan.Commands.Values.Single(command => command.Name == "TenantValue").Id.ToString());
    [Fact] void should_name_the_requirement_when_no_rule_name_is_available() => _admission.Blocking.Single().Details.ShouldEqual($"STAGE-ESM-028: Command 'TenantValue' generates 'value' with 'generate rule' (requirement '{Requirement}') on concept 'TenantOrderId'; the rule has an implementation body, which Stage's interpreter never runs or replaces with a generated value — render the application.");
    [Fact] void should_identify_the_command_capability() => _admission.Blocking.Single().Kind.ShouldEqual("command");
}
