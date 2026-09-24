// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

using NativeScreenplayCompiler = Cratis.Screenplay.ScreenplayCompiler;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer.given;

public class a_root_string_key_slice : Specification
{
    protected const string Global = "from OrderCreated\n  key literal \"global\"\n  total = total";
    const string ProjectionLevelKeyWarning = "PLAY0381";
    protected ApplicationSet _context = null!;
    protected SliceSyntax _slice = null!;

    protected void Compile(string blocks, string queries = "query OrderById => OrderReadModel", string projectionKey = "")
    {
        const string template = """
            concept LookupKey : String
            type OrderKey
              number String
            module Sales
              feature Orders
                slice StateView Summary
                  event OrderCreated
                    number String
                    total Decimal
                  event OrderUpdated
                    number String
                    total Decimal
                  projection Order => OrderReadModel
                    PROJECTIONKEY
                    BLOCKS
                  QUERIES
            """;
        var source = template
            .Replace("PROJECTIONKEY", projectionKey, StringComparison.Ordinal)
            .Replace("BLOCKS", blocks.Replace("\n", "\n        ", StringComparison.Ordinal), StringComparison.Ordinal)
            .Replace("QUERIES", queries.Replace("\n", "\n      ", StringComparison.Ordinal), StringComparison.Ordinal);
        var compilation = new NativeScreenplayCompiler().Compile(source);
        Assert.True(compilation.Success, $"{string.Join(Environment.NewLine, compilation.Diagnostics)}{Environment.NewLine}{source}");

        // A deliberately declared projection-level key is exactly what Screenplay warns about (PLAY0381: it
        // does not route events). Stage must still reject it on its own terms, so only that warning is expected.
        compilation.Diagnostics
            .Where(_ => projectionKey.Length == 0 || _.Code != ProjectionLevelKeyWarning)
            .ShouldBeEmpty();
        _context = new([compilation.Value!]);
        _slice = _context.Slices.Single().Slice;
    }

    protected RenderedFile Render() => new StateViewSliceRenderer().Render(new(_slice, ["Sales", "Orders"]), _context, "OrdersApp");
}
