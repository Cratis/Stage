// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.given;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_linked_sources;

public class with_parent_traversal_after_a_link : a_source_tree
{
    void Establish() => Directory.CreateSymbolicLink(Path.Combine(_root, "linked"), Path.Combine(_outside, "nested"));
    async Task Because() => _result = await SemanticModelLoader.LoadAsync(_root, ["linked/../nested/source.play"], null, "Shop");
    [SourceLinkFact] void should_refuse_the_link_before_normalizing_parent_traversal() => _result.Diagnostics.Single().Code.ShouldEqual("STAGE-PLAN-001");
    [SourceLinkFact] void should_name_the_normalized_root_relative_source() => _result.Diagnostics.Single().Source.ShouldEqual("nested/source.play");
    [SourceLinkFact] void should_not_compile_the_normalized_plain_file() => _result.Loaded.ShouldBeNull();
}
#endif
