// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_release_baseline;

public class with_the_file_and_folder_api : given.a_release_baseline_tree
{
    LoadedSemanticModel _model = null!;
    async Task Because() => _model = await SemanticModelLoader.LoadFromPathAsync(_root, Path.Combine(_root, ".screenplay", "identities.json"));
    [Fact] void should_use_the_persisted_application_name() => _model.Model.Application.Name.ShouldEqual("RecordsApp");
    [Fact] void should_check_the_baseline() => _model.ReleaseBaseline.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Checked);
    [Fact] void should_assess_the_loaded_revision() => _model.ReleaseBaseline.ModelRevision.ShouldEqual(_model.Model.Revision);
}
#endif
