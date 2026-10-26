// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
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
        var model = SemanticModelSerializer.Deserialize(PublicEventsCorpus.CapturesFromEventsV9.EsmBytes.AsSpan());
        var capture = model.Application.Modules.Single().Features.Single().Slices.Single().Captures.Single() with { Appends = [], Children = [], Nested = [] };
        var module = _plan.Model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single(candidate => candidate.Specifications.Any(specification => specification.Id == _specification.Id));
        var source = new SemanticCaptureEventsSource([slice.Events[0].Id]);
        typeof(SemanticCaptureEventsSource).GetProperty(nameof(SemanticCaptureEventsSource.KeyIsEventSource))!.SetValue(source, true);
        capture = capture with { EventsSource = source };
        var other = slice with { Id = capture.Id, Specifications = [], Commands = [], Events = [], ReadModels = [], Projections = [], Captures = [capture] };
        var slices = selected ? new[] { slice with { Captures = [capture] } } : [slice, other];
        var plan = WithApplication(application => application with { Modules = [module with { Features = [feature with { Slices = [.. slices] }] }] }, 32, 1);
        var report = await new SemanticSpecificationExecutor().Run(plan, new([_specification.Id]), new());
        var result = report.Results.Single();
        if (selected)
        {
            result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
            result.Unsupported!.Construct.ShouldEqual(slice.Id.ToString());
            result.Unsupported.Details.Contains("SemanticCaptureEventsSource.KeyIsEventSource", StringComparison.Ordinal).ShouldBeTrue();
        }
        else
        {
            result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
        }
    }
}
