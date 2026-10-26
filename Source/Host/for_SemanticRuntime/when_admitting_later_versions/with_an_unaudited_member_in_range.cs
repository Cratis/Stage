// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_an_unaudited_member_in_range : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish() => _plan = a_plan_at_an_esm_pair.From(32, 1, application => application with { Policies = [new("Sensitive policy", new SemanticAnonymousCondition())] });

    void Because() => _admission = new(_plan);

    [Fact] void should_refuse_the_model() => _admission.Blocking.Any(entry => entry.Details!.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_refuse_for_the_owner() => _admission.Blocking.First(entry => entry.Details!.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal)).Artifact.ShouldEqual(_plan.Model.Application.Id.ToString());
    [Fact] void should_name_the_member() => _admission.Blocking.First(entry => entry.Details!.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal)).Details!.Contains("SemanticAnonymousCondition.$type", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_leak_the_declaration_name() => _admission.Blocking.Any(entry => entry.Details!.Contains("Sensitive policy", StringComparison.Ordinal)).ShouldBeFalse();
}
