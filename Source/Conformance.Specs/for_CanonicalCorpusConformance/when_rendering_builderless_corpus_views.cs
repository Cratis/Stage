// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance;

public class when_rendering_builderless_corpus_views
{
    [Theory]
    [InlineData("EsmGoldenVectors.V14")]
    [InlineData("EsmGoldenVectors.V15")]
    [InlineData("EsmGoldenVectors.V16")]
    [InlineData("EsmGoldenVectors.V17")]
    [InlineData("EsmGoldenVectors.V18")]
    [InlineData("EsmGoldenVectors.V19")]
    [InlineData("EsmGoldenVectors.V20")]
    [InlineData("EsmGoldenVectors.V21")]
    [InlineData("EsmGoldenVectors.V22")]
    [InlineData("EsmGoldenVectors.V23")]
    [InlineData("EsmGoldenVectors.V24")]
    [InlineData("QueryFiltersCorpus.V14")]
    [InlineData("QueryValidationCorpus.V18")]
    public async Task should_not_refuse_a_missing_builder(string name)
    {
        var vector = canonical_corpus_vectors.All.Single(vector => vector.Name == name);
        var actual = await conformance_outcomes.Execute(conformance_surface.Renderer, vector);
        Assert.DoesNotContain(actual.Refusals, refusal => refusal.Code == "STAGE-ESM-007");
        Assert.DoesNotContain("gap:", actual.Outcome, StringComparison.Ordinal);
    }
}
#endif
