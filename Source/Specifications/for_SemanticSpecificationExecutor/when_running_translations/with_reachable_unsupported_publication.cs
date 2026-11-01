// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_translations;

public class with_reachable_unsupported_publication : Specification
{
    // Screenplay decision 0115: the outbound publisher belongs to another slice, so even when the capture's output reaches it, it does
    // not run for this specification and its unsupported shape refuses nothing.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_follow_capture_outputs_without_refusing_another_slices_publisher(bool reachable)
    {
        var captured = SemanticModelSerializer.Deserialize(PublicEventsCorpus.CapturesFromEventsV9.EsmBytes.AsSpan());
        var published = SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan());
        var capturedModule = captured.Application.Modules.Single();
        var capturedSlice = capturedModule.Features.Single().Slices.Single();
        var publishedModule = published.Application.Modules.Single();
        var publishedFeature = publishedModule.Features.Single();
        var outbound = publishedFeature.Slices.Single(slice => slice.Direction == SemanticTranslationDirection.Outbound);
        var projection = outbound.Projections.Single();
        var source = reachable ? capturedSlice.Events.Single(@event => @event.Name == "Added").Id : projection.Scope!.From[0].EventContract;
        projection = projection with
        {
            Scope = projection.Scope! with
            {
                From = [projection.Scope.From[0] with { EventContract = source, Mappings = [] }]
            }
        };
        outbound = outbound with { Projections = [projection], Specifications = [] };
        var application = captured.Application with
        {
            Concepts = published.Application.Concepts,
            Modules = [capturedModule, publishedModule with { Features = [publishedFeature with
            { Slices = [.. publishedFeature.Slices.Where(slice => slice.Kind == SemanticSliceKind.StateChange), outbound] }] }]
        };
        var plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(captured.LanguageVersion, captured.SemanticVersion, application)).Plan!;
        var specification = plan.Specifications.Values.Single(candidate => candidate.Name == "FirstArrival");
        var report = await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new());
        var actual = report.Results.Single();
        actual.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
        actual.Unsupported.ShouldBeNull();
    }
}
