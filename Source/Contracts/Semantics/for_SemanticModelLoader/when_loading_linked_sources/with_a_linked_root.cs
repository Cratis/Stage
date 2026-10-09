// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.given;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_linked_sources;

public class with_a_linked_root : a_source_tree
{
    string _linkedRoot = null!;
    void Establish()
    {
        _linkedRoot = Path.Combine(_workspace, "linked-root");
        Directory.CreateSymbolicLink(_linkedRoot, _root);
    }
    async Task Because() => _result = await SemanticModelLoader.LoadAsync(_linkedRoot, [], null, "Shop");
    [SourceLinkFact] void should_trust_the_supplied_root() => _result.Success.ShouldBeTrue();
    [SourceLinkFact] void should_compile_the_plain_sources_under_it() => _result.Loaded!.Plan.Commands.Count.ShouldEqual(1);
}
#endif
