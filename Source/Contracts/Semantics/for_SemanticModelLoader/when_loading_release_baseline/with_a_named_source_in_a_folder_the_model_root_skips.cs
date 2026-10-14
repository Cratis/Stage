// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_release_baseline;

public class with_a_named_source_in_a_folder_the_model_root_skips : given.a_release_baseline_tree
{
    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(_root, "bin"));
        File.WriteAllText(Path.Combine(_root, "bin", "extra.play"), "concept Extra : String");
    }

    async Task Because() => _loaded = await SemanticModelLoader.LoadAsync(_root, ["source.play", "bin/extra.play"], ".screenplay/identities.json", "RecordsApp");
    [Fact] void should_not_load_the_model() => _loaded.Success.ShouldBeFalse();
    [Fact] void should_refuse_the_unread_source() => _loaded.Diagnostics.Single().Code.ShouldEqual("STAGE-PLAN-001");
    [Fact] void should_name_the_unread_source() => _loaded.Diagnostics.Single().Source.ShouldEqual("bin/extra.play");
}
#endif
