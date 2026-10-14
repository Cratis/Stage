// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_release_baseline;

public class with_only_sources_the_model_root_skips : given.a_release_baseline_tree
{
    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(_root, "bin"));
        File.WriteAllText(Path.Combine(_root, "bin", "extra.play"), "concept Extra : String");
    }

    async Task Because() => _loaded = await SemanticModelLoader.LoadAsync(_root, ["bin"], ".screenplay/identities.json", "RecordsApp");
    [Fact] void should_not_load_the_model() => _loaded.Success.ShouldBeFalse();
    [Fact] void should_report_no_sources() => _loaded.Diagnostics.Single().Code.ShouldEqual("STAGE-PLAN-002");
}
#endif
