// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.given;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_a_source_directory_link_creates_a_cycle : given.a_source_plan
{
    void Establish() => Directory.CreateSymbolicLink(Path.Combine(_root, "cycle"), _root);
    async Task Because() => _result = await From(".");
    [SourceLinkFact] void should_refuse_the_cycle_without_recursing_forever() => ShouldRefuse("STAGE-PLAN-001");
    [SourceLinkFact] void should_not_render_any_artifacts() => _result.Artifacts.ShouldBeEmpty();
}
