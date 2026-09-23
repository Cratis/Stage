// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenDirectiveConverter.when_converting;

/// <summary>
/// A table or a summary is backed by a read model, and a running Stage attaches an element's API route by
/// matching a registered endpoint against exactly one property - <see cref="SceneElementProperties.TypeName"/>
/// - regardless of which converter produced the element. A table naming its read model under any other
/// property key would render correctly and never receive a route: not an error, just a screen that shows
/// column headers and nothing beneath them, which is indistinguishable from a model with no data yet.
/// </summary>
public class and_a_table_or_summary_names_a_read_model : Specification
{
    ExternalComponent _table = null!;
    ExternalComponent _summary = null!;

    void Because()
    {
        _table = (ExternalComponent)ScreenDirectiveConverter.Convert(
            [new ScreenTableSyntax("InvoiceSummary", [new ScreenColumnSyntax("amount", "Amount", SourceLocation.Start)], null, SourceLocation.Start)],
            "InvoiceList")[0];

        _summary = (ExternalComponent)ScreenDirectiveConverter.Convert(
            [new ScreenSummarySyntax("InvoiceSummary", [new ScreenFieldSyntax("amount", "Amount", SourceLocation.Start)], SourceLocation.Start)],
            "InvoiceList")[0];
    }

    [Fact] void should_carry_the_tables_read_model_under_the_property_a_route_resolves_against() =>
        _table.Properties[SceneElementProperties.TypeName].ShouldEqual("InvoiceSummary");

    [Fact] void should_carry_the_summarys_read_model_under_the_same_property() =>
        _summary.Properties[SceneElementProperties.TypeName].ShouldEqual("InvoiceSummary");
}
