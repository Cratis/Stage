// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_a_selection_has_no_slices : given.a_source_plan
{
    void Establish()
    {
        var model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with { Modules = [_module with { Features = [] }] });
        _loaded = new LoadedSemanticModel(model, SemanticExecutionPlan.Compile(model).Plan!);
    }
    void Because() => _result = CratisRendering.PlanFrom(_loaded, new([PlanSelectionEntry.Module("Projects")]), _planOptions);
    [Fact] void should_refuse_a_silently_empty_plan() => ShouldRefuse("STAGE-PLAN-014");
    [Fact] void should_not_render_any_artifacts() => _result.Artifacts.ShouldBeEmpty();
}
