// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Workspaces;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_release_baseline;

public class and_the_sidecar_is_unreadable : given.a_release_baseline_tree
{
    void Establish() => File.WriteAllText(Path.Combine(_root, ".screenplay", ReleaseBaselineStore.FileName), "not json");
    async Task Because() => _loaded = await SemanticModelLoader.LoadAsync(_root, [], ".screenplay/identities.json", "RecordsApp");
    [Fact] void should_still_load_the_model() => _loaded.Success.ShouldBeTrue();
    [Fact] void should_report_unreadable() => _loaded.Loaded!.ReleaseBaseline.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Unreadable);
    [Fact] void should_not_repair_the_sidecar() => File.ReadAllText(Path.Combine(_root, ".screenplay", ReleaseBaselineStore.FileName)).ShouldEqual("not json");
}
#endif
