// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSurfaceLedger;

public class when_classifying_query_shapes : Specification
{
    (SemanticQueryCardinality, SemanticQueryDelivery, bool)[] _rendered = [];
    SemanticSurfaceDisposition[] _refused = [];

    void Because()
    {
        _rendered = [.. SemanticSurfaceLedger.QueryShapes.Where(entry => entry.Value.Kind == SemanticSurfaceDispositionKind.Rendered).Select(entry => entry.Key)];
        _refused = [.. SemanticSurfaceLedger.QueryShapes.Where(entry => entry.Value.Kind == SemanticSurfaceDispositionKind.Rejected).Select(entry => entry.Value)];
    }

    [Fact] void should_render_only_snapshot_lookups_and_live_lists() => _rendered.ShouldContainOnly(
        (SemanticQueryCardinality.ZeroOrOne, SemanticQueryDelivery.Snapshot, true),
        (SemanticQueryCardinality.Many, SemanticQueryDelivery.Live, false),
        (SemanticQueryCardinality.Many, SemanticQueryDelivery.Live, true));
    [Fact] void should_refuse_all_other_combinations_with_the_query_shape_code() => _refused.Select(_ => _.Detail).Distinct().ShouldContainOnly("STAGE-ESM-010");
    [Fact] void should_cover_the_entire_shape_matrix() => (_rendered.Length + _refused.Length).ShouldEqual(Enum.GetValues<SemanticQueryCardinality>().Length * Enum.GetValues<SemanticQueryDelivery>().Length * 2);
    [Fact] void should_inventory_the_default_scene_list_omission() => SemanticSurfaceLedger.DefaultSceneListQueries.Kind.ShouldEqual(SemanticSurfaceDispositionKind.Ignored);
}
