// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_an_empty_scope : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;

    async Task Because()
    {
        var empty = _plan.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Specifications.IsEmpty);
        _report = await new SemanticSpecificationExecutor().Run(_plan, new([empty.Id]), new());
    }

    [Fact] void should_return_no_results() => _report.Results.Count.ShouldEqual(0);
}
