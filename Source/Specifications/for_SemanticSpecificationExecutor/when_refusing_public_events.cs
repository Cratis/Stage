// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_refusing_public_events : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;

    void Establish()
    {
        var original = SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan());
        var module = original.Application.Modules.Single();
        var feature = module.Features.Single();
        var publisher = feature.Slices.Single(slice => slice.Direction == SemanticTranslationDirection.Outbound);
        var specification = publisher.Specifications.Single();
        specification = specification with
        {
            GivenEvents = [specification.ThenEvents.Single()]
        };
        var application = original.Application with
        {
            Modules = [module with { Features = [feature with
            {
                Slices = [.. feature.Slices.Select(slice => slice with
                {
                    Kind = slice.Kind == SemanticSliceKind.Translate ? SemanticSliceKind.StateView : slice.Kind,
                    Direction = null,
                    Projections = [],
                    Captures = [],
                    Specifications = slice.Kind == SemanticSliceKind.StateChange ? [specification] : []
                })]
            }] }]
        };
        _plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(original.LanguageVersion, original.SemanticVersion, application)).Plan!;
    }

    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_plan, new([.. _plan.Specifications.Keys]), new());

    [Fact] void should_refuse_instead_of_running_the_public_event_given() => _report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_report_publication_not_version() => _report.Results.Single().Unsupported!.Details.StartsWith("STAGE-ESM-031:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_execute_nothing() => _report.Results.Single().Trace.ShouldBeNull();
}
