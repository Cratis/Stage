// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Screenplay;

namespace Cratis.Stage.Contracts.for_ReadModelConverter.given;

public class a_slice_with_projections : Specification
{
    protected readonly ConversionDiagnostics _diagnostics = new();
    protected readonly SchemaSynthesizer _schema = new(new Dictionary<string, ConceptSyntax>());
    protected SliceSyntax _slice = null!;
    protected ApplicationSyntax _application = null!;

    void Establish()
    {
        var location = new SourceLocation(5, 9, "items.play");
        var projection = new ProjectionSyntax("ItemsProjection", "Items", "audit", AutoMapMode.Inherit, null, [], location);
        _slice = new(Cratis.Screenplay.Syntax.SliceType.StateView, "Listing", [], [], [], [projection], [], [], [], [], [], location);
        var supported = _slice with { Name = "Other", Projections = [projection with { Name = "OtherProjection", Sequence = null }] };
        var feature = new FeatureSyntax("Browsing", [], [_slice, supported], location);
        _application = new([], [], [], [new ModuleSyntax("Store", [], [feature], location)], location);
    }
}
