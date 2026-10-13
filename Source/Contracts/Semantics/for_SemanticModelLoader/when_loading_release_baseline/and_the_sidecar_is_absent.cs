// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Workspaces;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_release_baseline;

public class and_the_sidecar_is_absent : given.a_release_baseline_tree
{
    void Establish() => File.Delete(Path.Combine(_root, ".screenplay", ReleaseBaselineStore.FileName));
    async Task Because() => _loaded = await SemanticModelLoader.LoadAsync(_root, [], null, "RecordsApp");
    [Fact] void should_report_absence() => _loaded.Loaded!.ReleaseBaseline.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Absent);
    [Fact] void should_not_create_a_baseline() => File.Exists(Path.Combine(_root, ".screenplay", ReleaseBaselineStore.FileName)).ShouldBeFalse();
}
#endif
