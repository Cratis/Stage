// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_semantic_exclusions;

public class and_retaining_empty_parents : given.an_excluded_feature
{
    void Establish()
    {
        var nestedId = SemanticId.Create(SemanticAddress.ForFeature(ApplicationIdentity.Create("Projects"), "Projects", ["Registration", "Nested"]));
        var nested = _feature with { Id = nestedId, Name = "Nested", Slices = [], Features = [] };
        var feature = _feature with { Slices = [], Features = [nested] };
        var application = _model.Application with { Modules = [_module with { Features = [feature] }] };
        var model = ExecutableSemanticModel.Create(_model.Language, _model.Semantics, application);
        _request = _request with
        {
            Model = model,
            ExecutionPlan = SemanticExecutionPlan.Compile(model).Plan,
            Exclusions = [_exclusion, _exclusion with { Address = "Nested unit", ParentId = nestedId },
                _exclusion with { Kind = SemanticExclusionKind.Policy, Address = "Module policy", ParentId = _module.Id },
                _exclusion with { Kind = SemanticExclusionKind.Import, Address = "Application import", ParentId = null }]
        };
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_keep_the_empty_feature_comment() => _plan.Artifacts.Any(artifact => artifact.RelativePath == "Projects/Registration/ScreenplayExclusions.g.cs").ShouldBeTrue();
    [Fact] void should_walk_nested_features_without_selected_slices() => _plan.Artifacts.Any(artifact => artifact.RelativePath == "Projects/Registration/Nested/ScreenplayExclusions.g.cs").ShouldBeTrue();
    [Fact] void should_place_module_evidence_in_the_module() => _plan.Artifacts.Any(artifact => artifact.RelativePath == "Projects/ScreenplayExclusions.g.cs").ShouldBeTrue();
    [Fact] void should_fall_back_to_the_application_root() => _plan.Artifacts.Single(artifact => artifact.RelativePath == "ScreenplayExclusions.g.cs").Sources.Single().ShouldEqual(_model.Application.Id);
    [Fact] void should_keep_each_parent_owned_warning() => _plan.PlanEvidence.Length.ShouldEqual(4);
}
