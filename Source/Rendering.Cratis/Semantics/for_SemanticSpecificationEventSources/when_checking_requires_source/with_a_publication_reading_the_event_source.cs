// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationEventSources.when_checking_requires_source;

public class with_a_publication_reading_the_event_source : a_source_observability_check
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_require_a_source_for_either_identity_value(bool eventContext)
    {
        var model = SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan());
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single(candidate => candidate.Projections.Length > 0);
        var projection = slice.Projections.Single();
        var scope = projection.Scope!;
        var contribution = scope.From.Single();
        var observing = contribution with
        {
            Mappings = contribution.Mappings.SetItem(0, contribution.Mappings[0] with
            {
                Source = eventContext ? new SemanticProjectionEventContextValue("eventSourceId") : new SemanticProjectionEventSourceIdentity()
            })
        };
        var changed = slice with { Projections = [projection with { Scope = scope with { From = [observing] } }] };
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = feature.Slices.Replace(slice, changed) }] }] };
        var candidate = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
        Check(candidate, contribution.EventContract, slice.Id).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_a_sourceless_given_observed_by_a_valid_publication()
    {
        var model = SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan());
        var slice = SemanticVersionFeatures.Slices(model.Application).Single(candidate => candidate.Specifications.Length > 0);
        var appended = slice.Specifications[0].WhenAppended!;
        var context = CreateContext(model, slice.Id);
        var fact = new SemanticSpecificationEvent(appended.EventContract, appended.Values);
        var exception = Assert.Throws<InvalidOperationException>(() =>
            SemanticSpecificationEventSources.Expression(context, new SemanticTypeSystem(context), fact));
        exception.Message.ShouldEqual("An observable event source must be supplied before rendering a specification.");
    }
}
