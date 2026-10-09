// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_queries;

public class with_an_unkeyed_snapshot_list : given.a_query_shape
{
    void Establish() => Configure(SemanticQueryCardinality.Many, SemanticQueryDelivery.Snapshot, false);
    void Because() => Evaluate();

    [Fact] void should_refuse_the_query_shape() => _diagnostics.Select(_ => _.Code).ShouldContainOnly("STAGE-ESM-010");
    [Fact] void should_locate_the_query() => _diagnostics.Single().Artifact.ShouldEqual(_query.Id);
}
