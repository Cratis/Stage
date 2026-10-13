// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_reaction_identity;

public class with_an_opaque_gate : given.a_gated_reaction
{
    void Establish() => WithGate("```csharp\n  return true;\n  ```");
    void Because() => Plan();
    [Fact] void should_refuse_authorization_with_unknown_claim_reads() => SemanticCratisAdmission.Evaluate(_context, [_located]).Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-034");
    [Fact] void should_plan_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
