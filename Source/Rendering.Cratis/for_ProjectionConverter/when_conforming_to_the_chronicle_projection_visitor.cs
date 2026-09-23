// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Contracts.Screenplay;
using Cratis.Stage.Rendering.Cratis.for_ProjectionConverter.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ProjectionConverter;

public class when_conforming_to_the_chronicle_projection_visitor
{
    [Theory]
    [InlineData("from ItemRegistered key id\n  id = id\n  label = $causedBy.name", "caused-by")]
    [InlineData("from ItemRegistered\n  label = \"first\"\n  label = \"second\"\n  clear label", "duplicate-and-clear")]
    [InlineData("all\n  count total", "all-events")]
    [InlineData("from LineAdded\n  key InvoiceLineKey\n    invoiceId = invoiceId\n    number = number\n  parent invoiceId\n  amount = 12.5\n  active = true\nremove with LineRemoved key number\n  parent invoiceId", "composite-parent-removal-and-literals")]
    [InlineData("children lines identified by number\n  no automap\n  children allocations identified by id\n    from Allocated key id\n      amount = amount\n  every\n    exclude children\n    changed = true", "recursive-children-and-explicit-mode")]
    [InlineData("nested shipping\n  no automap\n  every\n    label = label\n  from Shipped\n    city = city", "nested-explicit-mode")]
    [InlineData("every\n  lastSeen = $eventContext.occurred\nevery\n  lastSeen = $eventSourceId", "top-level-every-replacement")]
    [InlineData("from ItemRegistered+2 key id\n  id = id", "versioned-event")]
    [InlineData("from ItemRegistered\n  label = null\n  clear label\nremove with ItemRemoved\nremove via join on ItemDetached", "null-clear-and-removal")]
    [InlineData("nested shipping\n  from Shipped\n    city = city\n  nested carrier\n    from CarrierAssigned\n      name = name\n    clear with CarrierCleared", "recursive-nested")]
    [InlineData("children lines identified by lineNumber\n  from LineAdded key lineNumber\n    number = lineNumber\n  every\n    first = first\n  every\n    second = second\n  nested detail\n    from LineChanged\n      note = note", "child-every-merge-and-nested")]
    [InlineData("children lines identified by lineNumber\n  every\n    no automap\n    first = first\n  every\n    second = second", "child-every-inherits-prior-mode")]
    [InlineData("children lines identified by lineNumber\n  from LineAdded key lineNumber\n    number = lineNumber\n  every\n    second = second", "root-disabled-inherited-mode")]
    [InlineData("from ItemRegistered\n  label = label\njoin labels on label\n  with LabelChanged\n    label = name", "joined-mapping")]
    [InlineData("from ItemRegistered\n  key literal \"global\"\n  label = `by ${$causedBy.name}`\nremove with ItemRemoved key literal \"global\"", "literal-key-template-and-removal")]
    [InlineData("from ItemRegistered\n  label = label\n  add total by amount\n  subtract remaining by amount\n  increment count\n  decrement pending\n  count events", "arithmetic-mappings")]
    [InlineData("children lines identified by number\n  join labels on number\n    with LabelChanged\n      label = name\n  all\n    count total\n  remove via join on ItemDetached\n  clear with ItemRemoved", "child-join-all-remove-and-clear")]
    [InlineData("nested shipping\n  from Shipped\n    city = city\n  children lines identified by number\n    from LineAdded key number\n      number = number", "nested-children")]
    public void should_match_the_kernel_visitor(string blocks, string caseName)
    {
        var projection = CompileProjection(caseName == "versioned-event" ? blocks.Replace("+2", string.Empty, StringComparison.Ordinal) : blocks, caseName);
        if (caseName == "versioned-event")
        {
            var subscription = (FromSyntax)projection.Blocks.Single();
            projection = projection with { Blocks = [subscription with { Events = [subscription.Events.Single() with { Event = "ItemRegistered+2" }] }] };
        }
        if (caseName == "root-disabled-inherited-mode")
        {
            projection = projection with { AutoMap = AutoMapMode.Disabled };
        }

        var expected = ProjectionShape.Chronicle(ChronicleProjectionOracle.Visit(projection));
        var actual = ProjectionShape.Stage(ProjectionConverter.Convert(projection));
        Assert.Equal(expected, actual);
    }

    static ProjectionSyntax CompileProjection(string blocks, string caseName)
    {
        var source = "module Catalog\n  feature Items\n    slice StateView Summary\n      projection Summary => SummaryModel\n" +
            string.Join('\n', blocks.Split('\n').Select(_ => $"        {_}"));
        var compilation = new ScreenplayCompiler().Compile(source);
        Assert.True(compilation.Success, $"{caseName}: {string.Join(Environment.NewLine, compilation.Diagnostics)}");
        return compilation.Value!.Modules.Single().Features.Single().Slices.Single().Projections.Single();
    }
}
