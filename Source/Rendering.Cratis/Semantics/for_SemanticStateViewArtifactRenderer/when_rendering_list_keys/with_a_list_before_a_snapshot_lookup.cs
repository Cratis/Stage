// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer.when_rendering_list_keys;

public class with_a_list_before_a_snapshot_lookup : for_SemanticCratisAdmission.given.a_query_shape
{
    string _source = string.Empty;

    void Establish()
    {
        var model = _slice.ReadModels.Single();
        var key = model.Properties.Single(property => property.IsIdentifier);
        var foreignKey = model.Properties.First(property => !property.IsIdentifier);
        var text = foreignKey.Type with { Kind = SemanticTypeReferenceKind.Primitive, Primitive = SemanticPrimitiveType.Text, Target = default };

        // A primitive identifier needs [Key]; convert every matching declaration so the model stays valid.
        _registerProject = _registerProject with
        {
            Commands = [.. _registerProject.Commands.Select(command => command with
            {
                Properties = [.. command.Properties.Select(property => property.Type == key.Type ? property with { Type = text } : property)]
            })],
            Events = [.. _registerProject.Events.Select(@event => @event with
            {
                Properties = [.. @event.Properties.Select(property => property.Type == key.Type ? property with { Type = text } : property)]
            })]
        };
        var lookup = _query with { Argument = _query.Argument! with { Type = text } };
        Configure(SemanticQueryCardinality.Many, SemanticQueryDelivery.Live, true);
        _query = _query with { Name = "ListByName", Id = SemanticId.Parse($"sem1:{new string('a', 64)}"), Argument = new(SemanticId.Parse($"sem1:{new string('b', 64)}"), foreignKey.Name, foreignKey.Type), KeyProperty = foreignKey.Id };
        _slice = _slice with
        {
            Queries = [_query, lookup],
            ReadModels = [model with { Properties = [.. model.Properties.Select(property => property.Id == key.Id ? property with { Type = text } : property)] }]
        };
    }

    void Because()
    {
        _context = Context();
        _source = SemanticStateViewArtifactRenderer.Render(_context.Slice(_slice.Id), _context).Content;
    }

    [Fact] void should_select_the_lookup_key_even_after_a_list() => _source.Split("Cratis.Chronicle.Keys.KeyAttribute").Length.ShouldEqual(2);
    [Fact] void should_mark_the_identifier_instead_of_the_filter() => _source.ShouldContain("[global::Cratis.Chronicle.Keys.KeyAttribute] string ProjectId");
}
