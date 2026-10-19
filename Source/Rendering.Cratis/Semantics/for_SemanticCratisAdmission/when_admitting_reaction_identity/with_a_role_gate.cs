// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_reaction_identity;

public class with_a_role_gate : given.a_gated_reaction
{
    void Establish() => WithGate("role \"Automation\"");
    void Because() => Plan();
    [Fact] void should_admit_the_declared_identity() => _plan.Diagnostics.Where(diagnostic => diagnostic.Severity != global::Cratis.Stage.Contracts.Rendering.ArtifactRenderDiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_plan_the_reactor() => _plan.Artifacts.ShouldNotBeEmpty();
}
