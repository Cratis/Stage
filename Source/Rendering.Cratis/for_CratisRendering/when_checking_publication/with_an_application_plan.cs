// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_an_application_plan : given.a_policy_plan
{
    void Establish() => _existing["GeneratedPolicies/Policies.cs"] = LegacyPolicies;
    void Because() => _check = CratisRendering.CheckPublication(_application, Read);
    [Fact] void should_be_compatible() => _check.ShouldBeOfExactType<CratisPublicationCheck.Compatible>();
    [Fact] void should_read_only_event_destinations() => _readPaths.ShouldContainOnly(_application.Artifacts.Where(artifact => Text(artifact).Contains("[EventType", StringComparison.Ordinal)).Select(artifact => artifact.RelativePath));
    [Fact] void should_retain_application_scope() => _application.Scope.Kind.ShouldEqual(ArtifactRenderScopeKind.Application);
}
#endif
