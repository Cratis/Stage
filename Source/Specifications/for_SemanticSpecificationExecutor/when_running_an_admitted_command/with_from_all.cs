// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_from_all : a_command_only_plan
{
    SemanticSpecificationRunRecord _record = null!;

    async Task Because()
    {
        var application = _originalModel.Application;
        var projection = _originalModel.Application.Modules.SelectMany(module => module.Features)
            .SelectMany(feature => feature.Slices).SelectMany(slice => slice.Projections).Single();
        var scope = projection.Scope! with { Every = new(true, true, []) };
        var changed = application with
        {
            Modules = [.. application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Projections = [.. slice.Projections.Select(value => value.Id == projection.Id ? value with { Transitions = [], Scope = scope } : value)]
                    })]
                })]
            })]
        };
        var plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(_originalModel.LanguageVersion, _originalModel.SemanticVersion, changed)).Plan!;
        _record = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([_original.Id]), new())).Results);
    }

    [Fact] void should_reject_the_scoped_form_before_execution() => _record.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_share_the_renderers_from_all_rejection() => Assert.Contains("FromAll", _record.Unsupported!.Details);
}
