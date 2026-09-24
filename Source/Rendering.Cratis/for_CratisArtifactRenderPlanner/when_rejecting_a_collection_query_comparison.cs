// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rejecting_a_collection_query_comparison : Specification
{
    bool _supported;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("scopes"), "scopes", "Scopes.play", when_rendering_scoped_projections.ScopedSource);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success);
        var readModel = compilation.Value!.Model.Application.Modules.Single().Features.Single().Slices
            .Single(slice => slice.Kind == SemanticSliceKind.StateView).ReadModels.Single(model => model.Name == "ProjectSummary");
        var collection = readModel.Properties.Single(property => property.Name == "notes");
        _supported = SemanticSpecificationAdmission.CanCompareQueryResult([new(collection.Id, SemanticValue.Array([]))], readModel.Properties);
    }

    [Fact] void should_not_use_reference_equality_for_the_query_result() => _supported.ShouldBeFalse();
}
