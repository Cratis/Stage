// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_release_baseline;

public class with_a_bare_catalog : given.a_release_baseline_tree
{
    void Establish() => File.WriteAllBytes(Path.Combine(_root, "catalog.json"), SemanticIdentityCatalogSerializer.Serialize(_workspace.IdentityCatalog));
    async Task Because() => _loaded = await SemanticModelLoader.LoadAsync(_root, ["source.play"], "catalog.json", "RecordsApp");
    [Fact] void should_accept_the_bare_catalog() => _loaded.Success.ShouldBeTrue();
    [Fact] void should_check_the_baseline() => _loaded.Loaded!.ReleaseBaseline.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Checked);
    [Fact] void should_find_the_changed_revision() => _loaded.Loaded!.ReleaseBaseline.Violations.Single().Kind.ShouldEqual("BaselinedRevisionChanged");
}
#endif
