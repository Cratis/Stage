// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_release_baseline;

public class with_a_discovered_source_in_a_folder_the_model_root_skips : given.a_release_baseline_tree
{
    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(_root, "bin"));
        File.WriteAllText(Path.Combine(_root, "bin", "extra.play"), "concept Extra : String");
    }

    async Task Because() => _loaded = await SemanticModelLoader.LoadAsync(_root, [], ".screenplay/identities.json", "RecordsApp");
    [Fact] void should_load_the_model() => _loaded.Success.ShouldBeTrue();
    [Fact] void should_leave_the_skipped_source_out() => _loaded.Loaded!.Model.Application.Concepts.Any(concept => concept.Name == "Extra").ShouldBeFalse();
    [Fact] void should_check_the_baseline() => _loaded.Loaded!.ReleaseBaseline.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Checked);
}
#endif
