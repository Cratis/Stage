// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_release_baseline;

public class without_authoritative_identities : given.a_release_baseline_tree
{
    async Task Because() => _loaded = await SemanticModelLoader.LoadAsync(_root, [], null, "RecordsApp");
    [Fact] void should_still_load_the_model() => _loaded.Success.ShouldBeTrue();
    [Fact] void should_report_unavailable_instead_of_matching_provisional_identities() => _loaded.Loaded!.ReleaseBaseline.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Unavailable);
    [Fact] void should_not_invent_violations() => _loaded.Loaded!.ReleaseBaseline.Violations.ShouldBeEmpty();
}
#endif
