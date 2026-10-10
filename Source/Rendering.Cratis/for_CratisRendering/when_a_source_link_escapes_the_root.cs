// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.given;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_a_source_link_escapes_the_root : given.a_source_plan
{
    string _outside = null!;
    void Establish()
    {
        _outside = _root + "-outside.play";
        File.WriteAllText(_outside, Source);
        File.CreateSymbolicLink(Path.Combine(_root, "linked.play"), _outside);
    }
    async Task Because() => _result = await From("linked.play");
    [SourceLinkFact] void should_refuse_the_source_before_compilation() => ShouldRefuse("STAGE-PLAN-001");
    [SourceLinkFact] void should_not_render_any_artifacts() => _result.Artifacts.ShouldBeEmpty();
    void Destroy() => File.Delete(_outside);
}
