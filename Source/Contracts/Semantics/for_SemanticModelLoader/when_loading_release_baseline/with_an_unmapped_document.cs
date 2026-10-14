// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_release_baseline;

public class with_an_unmapped_document : given.a_release_baseline_tree
{
    void Establish() => File.WriteAllText(Path.Combine(_root, "unmapped.play"), "concept Other : String");
    async Task Because() => _loaded = await SemanticModelLoader.LoadAsync(_root, [], ".screenplay/identities.json", "RecordsApp");
    [Fact] void should_refuse_the_mapping_conflict() => _loaded.Success.ShouldBeFalse();
    [Fact] void should_report_an_unavailable_root() => _loaded.Diagnostics.Single().Code.ShouldEqual("STAGE-BASELINE-004");
}
#endif
