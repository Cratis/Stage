// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_recovering_a_query : given.a_rendered_application
{
    protected override string Source => ReadModelAbsenceCorpus.V5.SourceForms.Single(form => form.Name == "single").Documents.Single().Text;
    void Because() => Generate();

    [Fact] void should_recover_the_executable_query() => _recovered.Application.Modules.SelectMany(module => Slices(module.Features)).SelectMany(slice => slice.Queries).Select(query => query.Name).ShouldEqual(["InvoiceById"]);

    static IEnumerable<SemanticSlice> Slices(IEnumerable<SemanticFeature> features) =>
        features.SelectMany(feature => feature.Slices.Concat(Slices(feature.Features)));
}
#endif
