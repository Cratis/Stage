// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRunAdmission;

public class when_admitting_collecting_graph_queries : for_SemanticGraphViews.given.an_organization_with_views
{
    IEnumerable<SemanticAdmissionFeature> _features = null!;

    void Because() => _features = SemanticRunAdmission.ModelFeatures(_model);

    [Fact] void should_select_the_v54_pair() => (_model.LanguageVersion, _model.SemanticVersion).ShouldEqual((LanguageVersion.V54, SemanticVersion.V54));
    [Fact] void should_run_the_v54_pair() => SemanticRunAdmission.SupportsRun(_model).ShouldBeTrue();
    [Fact] void should_classify_every_member_of_the_collects() => SemanticRunAdmission.RunStanding(_model, new HashSet<SemanticId>()).UnknownMembers.ShouldBeEmpty();
    [Fact] void should_only_note_that_the_kernel_projects_the_graph() => _features.Select(feature => feature.Details).Single().ShouldContain($"{SemanticRunAdmission.GraphsNotProjected}:");
    [Fact] void should_not_block_the_model() => SemanticRunAdmission.BlockingModelFeatures(_application).ShouldBeEmpty();
}
