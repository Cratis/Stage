// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_CratisEsmRangeAdmission.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding;

public class with_gated_members : a_range_model
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_treat_only_a_non_default_gated_boolean_as_populated(bool populated)
    {
        var source = SemanticVersionFeatures.Slices(_model.Application).SelectMany(slice => slice.Captures).Single().EventsSource!;
        typeof(SemanticCaptureEventsSource).GetProperty(nameof(SemanticCaptureEventsSource.KeyIsEventSource))!.SetValue(source, populated);
        var found = FindGated("SemanticCaptureEventsSource.KeyIsEventSource");
        found.Any(member => member.Member == "SemanticCaptureEventsSource.KeyIsEventSource").ShouldEqual(populated);
        found.ShouldEachConformTo(member => member.SecurityRelevant);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_treat_only_a_populated_gated_collection_as_unknown(bool populated)
    {
        var slice = SemanticVersionFeatures.Slices(_model.Application).First(candidate => !candidate.Specifications.IsDefaultOrEmpty);
        var specification = slice.Specifications[0];
        ImmutableArray<SemanticSpecificationInvocation> invocations = populated ? [new(_model.Application.Id, [])] : [];
        typeof(SemanticSpecification).GetProperty(nameof(SemanticSpecification.ThenInvocations))!.SetValue(specification, invocations);
        FindGated().Any(member => member.Member == "SemanticSpecification.ThenInvocations").ShouldEqual(populated);
    }

    [Fact]
    void should_report_an_empty_anonymous_security_variant()
    {
        var application = _model.Application with { Policies = [new("Never disclose this policy", new SemanticAnonymousCondition())] };
        SetModel(ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, application));
        var finding = FindGated().Single(member => member.Member == "SemanticAnonymousCondition.$type");
        finding.SecurityRelevant.ShouldBeTrue();
        finding.Path.ShouldEqual("Application.Policies[0].Condition");
        finding.Owner.ShouldEqual(_model.Application.Id);
    }

    ImmutableArray<SemanticUnknownMember> FindGated(params string[] additionallyGated)
    {
        var gated = SemanticSurfaceLedger.Entries.Where(entry => entry.Value.Kind == SemanticSurfaceDispositionKind.Rejected && entry.Value.Detail == "STAGE-ESM-016")
            .Select(entry => entry.Key).Concat(additionallyGated).ToImmutableHashSet(StringComparer.Ordinal);
        var known = SemanticSurfaceLedger.Entries.Where(entry => !gated.Contains(entry.Key)).ToImmutableDictionary(StringComparer.Ordinal);
        return SemanticUnknownMembers.Find(_model, known, new(ImmutableHashSet<SemanticId>.Empty, ImmutableHashSet<SemanticId>.Empty, gated));
    }
}
