// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSurfaceLedger;

public class when_auditing_routes : Specification
{
    IReadOnlyDictionary<string, SemanticSurfaceDisposition> _entries = null!;

    void Because() => _entries = SemanticSurfaceLedger.Entries;

    [Fact] void should_classify_command_routes_as_rendered() => _entries["SemanticCommand.Route"].Kind.ShouldEqual(SemanticSurfaceDispositionKind.Rendered);
    [Fact] void should_classify_definitions_as_rendered() => _entries["SemanticApplication.EventSources"].Kind.ShouldEqual(SemanticSurfaceDispositionKind.Rendered);
    [Fact] void should_classify_route_assertions_as_rendered() => _entries["SemanticSpecificationEvent.Route"].Kind.ShouldEqual(SemanticSurfaceDispositionKind.Rendered);
    [Fact] void should_classify_unrouted_assertions_as_rendered() => _entries["SemanticSpecificationEvent.Unrouted"].Kind.ShouldEqual(SemanticSurfaceDispositionKind.Rendered);
    [Fact] void should_classify_fixture_parts_as_rendered() => _entries["SemanticFixtureRoutePart.Value"].Kind.ShouldEqual(SemanticSurfaceDispositionKind.Rendered);
    [Fact] void should_keep_direct_appends_refused() => _entries["SemanticSpecification.WhenAppended"].ShouldEqual(new SemanticSurfaceDisposition(SemanticSurfaceDispositionKind.Rejected, "STAGE-ESM-011"));
}
