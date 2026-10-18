// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_ProjectionConverter.when_converting_with_diagnostics;

public class and_a_construct_is_unsupported : Specification
{
    static readonly SourceLocation Location = new(12, 7, "views.play");

    [Theory]
    [InlineData("sequence", "STAGE-PROJECTION-001", "projection sequence 'audit'")]
    [InlineData("variant", "STAGE-PROJECTION-002", "projection variant")]
    [InlineData("block", "STAGE-PROJECTION-003", "projection block UnknownBlock")]
    [InlineData("mapping", "STAGE-PROJECTION-004", "projection mapping UnknownMapping on 'name'")]
    [InlineData("key", "STAGE-PROJECTION-006", "projection key UnknownKey")]
    [InlineData("automap", "STAGE-PROJECTION-007", "projection automap mode 99")]
    public void should_report_the_construct_and_omit_the_whole_projection(string kind, string code, string construct)
    {
        var diagnostics = new ConversionDiagnostics();
        var projection = Projection(kind);
        var result = ProjectionConverter.Convert(projection, diagnostics, "Store.Items.Listing");

        result.ShouldBeNull();
        diagnostics.HasErrors.ShouldBeTrue();
        diagnostics.All.Count.ShouldEqual(1);
        var diagnostic = diagnostics.All[0];
        diagnostic.Code.ShouldEqual(code);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
        diagnostic.Construct.ShouldEqual(construct);
        diagnostic.Path.ShouldEqual("Store.Items.Listing");
        diagnostic.Location.ShouldEqual(Location);
        diagnostic.ToDiagnostic().Code.ShouldEqual(code);
        diagnostic.ToDiagnostic().Location.ShouldEqual(Location);
        diagnostic.ToDiagnostic().Severity.ShouldEqual(DiagnosticSeverity.Error);
    }

    static ProjectionSyntax Projection(string kind) => new(
        "Items",
        "Items",
        kind == "sequence" ? "audit" : null,
        kind == "automap" ? (AutoMapMode)99 : AutoMapMode.Inherit,
        kind == "key" ? new UnknownKey(Location) : null,
        kind switch
        {
            "variant" => [new ProjectionVariantSyntax("Active", [], [], Location)],
            "block" => [new UnknownBlock(Location)],
            "mapping" => [new EverySyntax([new UnknownMapping("name", Location)], false, AutoMapMode.Inherit, Location)],
            _ => []
        },
        Location);

    sealed record UnknownBlock(SourceLocation Location) : ProjectionBlockSyntax(Location);
    sealed record UnknownMapping(string Property, SourceLocation Location) : MappingSyntax(Property, Location);
    sealed record UnknownKey(SourceLocation Location) : KeySyntax(Location);
}
