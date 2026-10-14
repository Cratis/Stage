// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_validating_recovery_specifications : a_recovery_plan
{
    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void should_refuse_an_out_of_range_given_position(int index) =>
        Catch.Exception(() => WithSpecification(_model, _specification with { WhenRedelivered = _specification.WhenRedelivered! with { Given = index } })).ShouldBeOfExactType<InvalidSemanticContract>();

    [Fact] void should_refuse_a_reaction_that_does_not_observe_the_given() => Catch.Exception(() => WithSpecification(_model, _specification with { WhenRedelivered = new(_plan.Reactions.Single(reaction => reaction.Name == "Finisher").Id, 0) })).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_no_events_alongside_event_expectations() => Catch.Exception(() => WithSpecification(_model, _specification with { ThenNoEvents = true })).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_recovery_members_on_v16() => Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V16, SemanticVersion.V16, _model.Application)).ShouldBeOfExactType<InvalidSemanticContract>();

    [Theory]
    [InlineData("order")]
    [InlineData("error")]
    [InlineData("denial")]
    public void should_refuse_incompatible_no_event_outcomes(string outcome)
    {
        var specification = _plan.Specifications.Values.Single(specification => specification.Name == "FilteredRedelivery");
        var conflicting = outcome switch
        {
            "order" => specification with { ThenEventsInAnyOrder = true },
            "denial" => specification with { ThenDenied = true },
            _ => specification with { ThenErrors = [new(null, "Refused")] }
        };
        Catch.Exception(() => WithSpecification(_model, conflicting)).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    public void should_refuse_an_ambiguous_source_locator()
    {
        var corpus = RecoverySpecificationsCorpus.V17;
        var form = corpus.SourceForms.Single(form => form.Name == "single");
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var original = form.Documents.Single();
        const string Given = "        given Approved\n          id = \"payment-a\"\n";
        var source = original.Text.Replace(Given, Given + Given, StringComparison.Ordinal);
        source.ShouldNotEqual(original.Text);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(original.StableKey), original.StableKey, original.DisplayPath, source);
        var compilation = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeFalse();
        compilation.Diagnostics.ShouldNotBeEmpty();
    }
}
#endif
