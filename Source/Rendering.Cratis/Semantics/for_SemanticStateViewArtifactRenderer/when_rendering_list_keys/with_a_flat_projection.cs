// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer.when_rendering_list_keys;

public class with_a_flat_projection : for_SemanticCratisAdmission.given.a_query_shape
{
    string _source = string.Empty;

    void Establish()
    {
        var foreignKey = _slice.ReadModels.Single().Properties.First(property => !property.IsIdentifier);
        Configure(SemanticQueryCardinality.Many, SemanticQueryDelivery.Live, true);
        _query = _query with { Argument = new(_query.Argument!.Id, foreignKey.Name, foreignKey.Type), KeyProperty = foreignKey.Id };
        _slice = _slice with { Queries = [_query] };
    }

    void Because()
    {
        _context = Context();
        _source = SemanticStateViewArtifactRenderer.Render(_context.Slice(_slice.Id), _context).Content;
    }

    [Fact] void should_keep_the_list_filter_out_of_the_instance_key() => _source.ShouldNotContain("Cratis.Chronicle.Keys.KeyAttribute");
    [Fact] void should_preserve_the_list_filter() => _source.ShouldContain("EqualityComparer<");
}
