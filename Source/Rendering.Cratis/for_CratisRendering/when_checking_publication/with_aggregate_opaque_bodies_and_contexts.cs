// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_aggregate_opaque_bodies_and_contexts : given.a_policy_plan
{
    void Establish()
    {
        // Before #211, the per-site body file's declarations lived at this aggregate path.
        _existing["GeneratedPolicies/PolicyBodies.cs"] = Text(_application.Artifacts.First(artifact => artifact.RelativePath.StartsWith("GeneratedPolicies/PolicyBodies_", StringComparison.Ordinal)));

        // Before #211, use-site wrappers accompanied the shared identity runtime in this file.
        _existing["TypedContexts/PolicyContext.cs"] = Text(_application.Artifacts.Single(artifact => artifact.RelativePath == "TypedContexts/PolicyContext.cs")) + "\npublic sealed record TypedContext_issue(string Subject, Identity Identity, global::System.DateTimeOffset Occurred);\n";
    }

    void Because() => _check = CratisRendering.CheckPublication(_plan, Read);
    [Fact] void should_require_application_scope() => _check.ShouldBeOfExactType<CratisPublicationCheck.RequiresApplicationScope>();
    [Fact] void should_name_both_aggregate_paths_in_ordinal_order() => ((CratisPublicationCheck.RequiresApplicationScope)_check).Paths.SequenceEqual(["GeneratedPolicies/PolicyBodies.cs", "TypedContexts/PolicyContext.cs"]).ShouldBeTrue();
}
#endif
