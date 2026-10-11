// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_routes;

public class and_a_read_model_companion_needs_routed_replay : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = routed_specifications.Source
            .Replace("produces Deposited\n          for accountId", "produces Deposited\n          for accountId\n          accountId = accountId", StringComparison.Ordinal)
            .Replace("event Deposited\n", "event Deposited\n        accountId String\n", StringComparison.Ordinal)
            .Replace("then Deposited\n", "then Deposited\n          accountId = \"acc-1\"\n", StringComparison.Ordinal) + "\n" + """
                    then readmodel Balance
                      accountId = "acc-1"
                  readmodel Balance
                    accountId String
                  query BalanceByAccount => Balance?
                    by accountId String
                  projection Balances => Balance
                    from Deposited key accountId
                      accountId = accountId
            """;
        _plan = routed_specifications.Plan(routed_specifications.Compile(source));
    }

    [Fact] void should_admit_routed_companion_replay() => _plan.Success.ShouldBeTrue();
    [Fact] void should_seed_the_command_route() => System.Text.Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("_is_projected.cs", StringComparison.Ordinal)).Bytes.AsSpan()).ShouldContain(".OnStream(eventSourceType: \"stored-account\", eventStreamType: \"stored-transactions\", eventStreamId: \"2026-10\")");
}
