// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.given;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_linked_sources;

public class with_a_listed_file_link : a_source_tree
{
    void Establish() => File.CreateSymbolicLink(Path.Combine(_root, "linked.play"), Path.Combine(_outside, "nested", "source.play"));
    async Task Because() => _result = await SemanticModelLoader.LoadAsync(_root, ["linked.play"], null, "Shop");
    [SourceLinkFact] void should_refuse_the_link() => _result.Diagnostics.Single().Code.ShouldEqual("STAGE-PLAN-001");
    [SourceLinkFact] void should_name_the_root_relative_source() => _result.Diagnostics.Single().Source.ShouldEqual("linked.play");
    [SourceLinkFact] void should_not_compile_the_target() => _result.Loaded.ShouldBeNull();
}
#endif
