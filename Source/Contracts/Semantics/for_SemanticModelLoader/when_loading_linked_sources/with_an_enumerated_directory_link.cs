// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.given;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_linked_sources;

public class with_an_enumerated_directory_link : a_source_tree
{
    void Establish() => Directory.CreateSymbolicLink(Path.Combine(_root, "linked"), _outside);
    async Task Because() => _result = await SemanticModelLoader.LoadAsync(_root, [], null, "Shop");
    [SourceLinkFact] void should_report_the_link_instead_of_skipping_it() => _result.Diagnostics.Single().Code.ShouldEqual("STAGE-PLAN-001");
    [SourceLinkFact] void should_name_the_root_relative_directory() => _result.Diagnostics.Single().Source.ShouldEqual("linked");
    [SourceLinkFact] void should_not_compile_partial_sources() => _result.Loaded.ShouldBeNull();
}
#endif
