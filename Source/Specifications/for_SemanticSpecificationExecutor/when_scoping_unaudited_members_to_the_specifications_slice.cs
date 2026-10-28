// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_scoping_unaudited_members_to_the_specifications_slice : a_plan_at_an_esm_pair
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_refuse_only_the_owning_slice(bool selected)
    {
        var module = _plan.Model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single(candidate => candidate.Specifications.Any(specification => specification.Id == _specification.Id));
        var specification = _specification with { Id = selected ? _specification.Id : SemanticId.Parse($"sem1:{1:x64}") };
        typeof(SemanticSpecification).GetProperty(nameof(SemanticSpecification.ThenInvocations))!.SetValue(specification, ImmutableArray.Create(new SemanticSpecificationInvocation(_specification.When!.Command, [])));
        var other = slice with { Id = SemanticId.Parse($"sem1:{2:x64}"), Specifications = [specification], Commands = [], Events = [], ReadModels = [], Projections = [] };
        var slices = selected ? new[] { slice with { Specifications = [specification] } } : [slice, other];
        var plan = WithApplication(application => application with { Modules = [module with { Features = [feature with { Slices = [.. slices] }] }] }, 32, 1);
        var report = await new SemanticSpecificationExecutor().Run(plan, new([_specification.Id]), new());
        var result = report.Results.Single();
        if (selected)
        {
            result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
            result.Unsupported!.Construct.ShouldEqual(slice.Id.ToString());
            result.Unsupported.Details.Contains("SemanticSpecification.ThenInvocations", StringComparison.Ordinal).ShouldBeTrue();
        }
        else
        {
            result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
        }
    }
}
