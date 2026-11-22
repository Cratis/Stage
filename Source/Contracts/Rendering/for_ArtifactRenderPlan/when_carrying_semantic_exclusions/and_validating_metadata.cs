// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_carrying_semantic_exclusions;

public class and_validating_metadata : given.a_request_with_exclusions
{
    [Fact] void should_reject_an_uninitialized_attachment() => Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderPlan.Create(_request with { Exclusions = default }, [], []));
    [Fact] void should_reject_duplicate_entries() => Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderPlan.Create(_request with { Exclusions = [_slice, _slice] }, [], []));
    [Fact] void should_reject_an_unset_address() => Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderPlan.Create(_request with { Exclusions = [_slice with { Address = " " }] }, [], []));
    [Fact] void should_reject_missing_diagnostics() => Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderPlan.Create(_request with { Exclusions = [_slice with { Diagnostics = [] }] }, [], []));
    [Fact] void should_reject_uninitialized_dependencies() => Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderPlan.Create(_request with { Exclusions = [_slice with { DependsOn = default }] }, [], []));
    [Fact] void should_reject_an_unknown_set_parent() => Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderPlan.Create(_request with { Exclusions = [_slice with { ParentId = SemanticId.Create(SemanticAddress.ForApplication(ApplicationIdentity.Create("Unrelated"))) }] }, [], []));
}
