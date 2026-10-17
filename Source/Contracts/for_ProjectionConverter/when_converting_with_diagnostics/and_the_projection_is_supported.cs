// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Projections;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_ProjectionConverter.when_converting_with_diagnostics;

public class and_the_projection_is_supported : Specification
{
    readonly ConversionDiagnostics _diagnostics = new();
    ProjectionDefinition? _result;

    void Because()
    {
        _diagnostics.Report(new("earlier", DiagnosticSeverity.Error, "earlier projection", "Other", SourceLocation.Start, "Earlier omission"));
        _result = ProjectionConverter.Convert(new("Items", "Items", null, AutoMapMode.Inherit, null, [], SourceLocation.Start), _diagnostics, "Listing");
    }

    [Fact] void should_convert_even_if_an_earlier_projection_failed() => _result.ShouldNotBeNull();
    [Fact] void should_keep_the_existing_automap_default() => _result!.AutoMap.ShouldEqual(ProjectionAutoMap.Enabled);
    [Fact] void should_add_no_diagnostics() => _diagnostics.All.Count.ShouldEqual(1);
}
