// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_reaction_identity;

public class with_a_claim_behind_or : given.a_gated_reaction
{
    void Establish() => WithGate("role \"Automation\" or claim \"sub\" matches \"[System]\"");
    void Because() => Plan();
    [Fact] void should_refuse_claim_reads_even_behind_a_role_alternative() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-034");
    [Fact] void should_plan_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
